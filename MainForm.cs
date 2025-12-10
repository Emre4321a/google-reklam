using System.Net;
using System.Net.Http;
using System.Drawing.Drawing2D;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace GoogleSearchApp;

public class MainForm : Form
{
    private TextBox txtKeyword = null!;
    private Button btnSearch = null!;
    private Button btnClickPages = null!;
    private Button btnStop = null!;
    private ListBox lstResults = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
    private CheckBox chkShowBrowser = null!;
    private CheckBox chkRotateIP = null!;
    private NumericUpDown numResultCount = null!;
    private NumericUpDown numLoopCount = null!;
    private PictureBox picLogo = null!;
    private Panel headerPanel = null!;
    private Panel mainPanel = null!;
    private RichTextBox txtLog = null!;
    private TabControl tabControl = null!;
    private RadioButton rdoDesktop = null!;
    private RadioButton rdoMobile = null!;
    private List<SearchResult> currentResults = new List<SearchResult>();
    private static readonly Random random = new Random();
    private List<ProxyInfo> proxyList = new List<ProxyInfo>();
    private List<ProxyInfo> workingProxyList = new List<ProxyInfo>();
    private int currentProxyIndex = 0;
    private bool stopRequested = false;
    private CancellationTokenSource? cancellationTokenSource;
    
    // Uygulama renkleri
    private readonly Color primaryColor = Color.FromArgb(41, 128, 185);      // Mavi
    private readonly Color secondaryColor = Color.FromArgb(52, 73, 94);      // Koyu gri-mavi
    private readonly Color accentColor = Color.FromArgb(46, 204, 113);       // Yeşil
    private readonly Color dangerColor = Color.FromArgb(231, 76, 60);        // Kırmızı
    private readonly Color bgColor = Color.FromArgb(236, 240, 241);          // Açık gri
    private readonly Color headerBgColor = Color.FromArgb(44, 62, 80);       // Koyu header

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Sponsor Botu";
        this.Size = new Size(920, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = bgColor;
        this.Icon = SystemIcons.Application;

        // ===== HEADER PANEL =====
        headerPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(920, 90),
            BackColor = headerBgColor
        };
        this.Controls.Add(headerPanel);

        // Logo PictureBox
        picLogo = new PictureBox
        {
            Location = new Point(15, 8),
            Size = new Size(70, 70),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        headerPanel.Controls.Add(picLogo);
        
        // Logo'yu internetten yükle
        LoadLogoAsync();

        // Başlık Label
        var lblTitle = new Label
        {
            Text = "Sponsor Botu",
            Location = new Point(95, 12),
            Size = new Size(250, 35),
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };
        headerPanel.Controls.Add(lblTitle);

        // Alt başlık
        var lblSubtitle = new Label
        {
            Text = "SEO Traffic Tool",
            Location = new Point(97, 48),
            Size = new Size(280, 22),
            Font = new Font("Segoe UI", 10, FontStyle.Italic),
            ForeColor = Color.FromArgb(189, 195, 199),
            BackColor = Color.Transparent
        };
        headerPanel.Controls.Add(lblSubtitle);

        // Sağ üst köşe bilgi
        var lblVersion = new Label
        {
            Text = $"v{UpdateChecker.CurrentVersion}",
            Location = new Point(800, 10),
            Size = new Size(100, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(149, 165, 166),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight
        };
        headerPanel.Controls.Add(lblVersion);

        // Güncelleme Kontrol Butonu
        var btnCheckUpdate = new Button
        {
            Text = "Güncelle",
            Location = new Point(800, 35),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnCheckUpdate.FlatAppearance.BorderSize = 0;
        btnCheckUpdate.Click += async (s, e) => await CheckForUpdatesManually();
        headerPanel.Controls.Add(btnCheckUpdate);

        // Bilgilendirme Butonu
        var btnInfo = new Button
        {
            Text = "Bilgi",
            Location = new Point(800, 62),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnInfo.FlatAppearance.BorderSize = 0;
        btnInfo.Click += (s, e) => ShowInfoDialog();
        headerPanel.Controls.Add(btnInfo);

        // ===== MAIN PANEL =====
        mainPanel = new Panel
        {
            Location = new Point(0, 90),
            Size = new Size(920, 630),
            BackColor = bgColor,
            Padding = new Padding(15)
        };
        this.Controls.Add(mainPanel);

        // Arama Grubu Paneli
        var searchGroupPanel = new Panel
        {
            Location = new Point(15, 10),
            Size = new Size(875, 55),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        searchGroupPanel.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, searchGroupPanel.Width - 1, searchGroupPanel.Height - 1);
        };
        mainPanel.Controls.Add(searchGroupPanel);

        // Anahtar Kelime Label
        var lblKeyword = new Label
        {
            Text = "Ara:",
            Location = new Point(10, 16),
            Size = new Size(35, 22),
            Font = new Font("Segoe UI", 10),
            ForeColor = secondaryColor
        };
        searchGroupPanel.Controls.Add(lblKeyword);

        // Anahtar Kelime TextBox
        txtKeyword = new TextBox
        {
            Location = new Point(45, 13),
            Size = new Size(320, 28),
            Font = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtKeyword.KeyDown += TxtKeyword_KeyDown;
        searchGroupPanel.Controls.Add(txtKeyword);

        // Sonuç Sayısı Label
        var lblCount = new Label
        {
            Text = "Sonuç:",
            Location = new Point(380, 16),
            Size = new Size(45, 23),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor
        };
        searchGroupPanel.Controls.Add(lblCount);

        // Sonuç Sayısı NumericUpDown
        numResultCount = new NumericUpDown
        {
            Location = new Point(425, 12),
            Size = new Size(50, 28),
            Font = new Font("Segoe UI", 10),
            Minimum = 1,
            Maximum = 20,
            Value = 3,
            BorderStyle = BorderStyle.FixedSingle
        };
        searchGroupPanel.Controls.Add(numResultCount);

        // Ara Butonu
        btnSearch = CreateStyledButton("Google'da Ara", new Point(490, 8), new Size(130, 38), primaryColor);
        btnSearch.Click += BtnSearch_Click;
        searchGroupPanel.Controls.Add(btnSearch);

        // Proxy Yenile Butonu
        var btnRefreshProxy = CreateStyledButton("Proxy Yenile", new Point(630, 8), new Size(120, 38), Color.FromArgb(155, 89, 182));
        btnRefreshProxy.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnRefreshProxy.Click += async (s, ev) => await RefreshProxyList();
        searchGroupPanel.Controls.Add(btnRefreshProxy);

        // Seçenekler Paneli
        var optionsPanel = new Panel
        {
            Location = new Point(15, 72),
            Size = new Size(875, 50),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        optionsPanel.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, optionsPanel.Width - 1, optionsPanel.Height - 1);
        };
        mainPanel.Controls.Add(optionsPanel);

        // Tarayıcıyı Göster CheckBox
        chkShowBrowser = new CheckBox
        {
            Text = "Göster",
            Location = new Point(12, 14),
            Size = new Size(65, 22),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor,
            Checked = false
        };
        optionsPanel.Controls.Add(chkShowBrowser);

        // IP Rotasyonu CheckBox
        chkRotateIP = new CheckBox
        {
            Text = "Proxy",
            Location = new Point(80, 14),
            Size = new Size(60, 22),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor,
            Checked = true
        };
        optionsPanel.Controls.Add(chkRotateIP);

        // Ayırıcı çizgi
        var separator1 = new Label
        {
            Text = "|",
            Location = new Point(145, 14),
            Size = new Size(10, 22),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        optionsPanel.Controls.Add(separator1);

        // Masaüstü RadioButton
        rdoDesktop = new RadioButton
        {
            Text = "Masaüstü",
            Location = new Point(158, 13),
            Size = new Size(80, 22),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor,
            Checked = true
        };
        optionsPanel.Controls.Add(rdoDesktop);

        // Mobil RadioButton
        rdoMobile = new RadioButton
        {
            Text = "Mobil",
            Location = new Point(240, 13),
            Size = new Size(60, 22),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor,
            Checked = false
        };
        optionsPanel.Controls.Add(rdoMobile);

        // Ayırıcı çizgi 2
        var separator2 = new Label
        {
            Text = "|",
            Location = new Point(305, 14),
            Size = new Size(10, 22),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        optionsPanel.Controls.Add(separator2);

        // Loop Sayısı Label
        var lblLoop = new Label
        {
            Text = "Döngü:",
            Location = new Point(318, 15),
            Size = new Size(45, 20),
            Font = new Font("Segoe UI", 9),
            ForeColor = secondaryColor
        };
        optionsPanel.Controls.Add(lblLoop);

        // Loop Sayısı NumericUpDown
        numLoopCount = new NumericUpDown
        {
            Location = new Point(365, 11),
            Size = new Size(55, 26),
            Font = new Font("Segoe UI", 10),
            Minimum = 1,
            Maximum = 1000,
            Value = 1,
            BorderStyle = BorderStyle.FixedSingle
        };
        optionsPanel.Controls.Add(numLoopCount);

        // Ayırıcı çizgi 3
        var separator3 = new Label
        {
            Text = "|",
            Location = new Point(428, 14),
            Size = new Size(10, 22),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        optionsPanel.Controls.Add(separator3);

        // Sayfalara Tıkla Butonu
        btnClickPages = CreateStyledButton("Sayfalara Tıkla", new Point(445, 8), new Size(130, 34), accentColor);
        btnClickPages.Enabled = false;
        btnClickPages.Click += BtnClickPages_Click;
        optionsPanel.Controls.Add(btnClickPages);

        // Durdur Butonu
        btnStop = CreateStyledButton("Durdur", new Point(585, 8), new Size(90, 34), dangerColor);
        btnStop.Enabled = false;
        btnStop.Click += BtnStop_Click;
        optionsPanel.Controls.Add(btnStop);

        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(15, 128),
            Size = new Size(875, 5),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };
        mainPanel.Controls.Add(progressBar);

        // Tab Control
        tabControl = new TabControl
        {
            Location = new Point(15, 138),
            Size = new Size(875, 345),
            Font = new Font("Segoe UI", 10),
        };
        mainPanel.Controls.Add(tabControl);

        // Sonuçlar Tab
        var tabResults = new TabPage
        {
            Text = "Sonuçlar",
            BackColor = Color.White,
            Padding = new Padding(5)
        };
        tabControl.TabPages.Add(tabResults);

        // Sonuçlar ListBox
        lstResults = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = secondaryColor
        };
        lstResults.DoubleClick += LstResults_DoubleClick;
        tabResults.Controls.Add(lstResults);

        // Log Tab
        var tabLog = new TabPage
        {
            Text = "Log Kayıtları",
            BackColor = Color.FromArgb(30, 30, 30),
            Padding = new Padding(5)
        };
        tabControl.TabPages.Add(tabLog);

        // Log TextBox
        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(0, 255, 0),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            WordWrap = false
        };
        tabLog.Controls.Add(txtLog);

        // Log Temizle Butonu
        var btnClearLog = new Button
        {
            Text = "Log Temizle",
            Dock = DockStyle.Bottom,
            Height = 30,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.Click += (s, e) => { txtLog.Clear(); Log("Log temizlendi.", LogLevel.Info); };
        tabLog.Controls.Add(btnClearLog);

        // Durum Paneli
        var statusPanel = new Panel
        {
            Location = new Point(15, 490),
            Size = new Size(875, 55),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        statusPanel.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, statusPanel.Width - 1, statusPanel.Height - 1);
        };
        mainPanel.Controls.Add(statusPanel);

        // Durum Label
        lblStatus = new Label
        {
            Text = "Arama yapmak için bir anahtar kelime girin ve 'Google'da Ara' butonuna tıklayın.",
            Location = new Point(15, 10),
            Size = new Size(810, 20),
            Font = new Font("Segoe UI", 10),
            ForeColor = primaryColor
        };
        statusPanel.Controls.Add(lblStatus);

        // İpucu Label
        var lblTip = new Label
        {
            Text = "İpucu: Sonuca çift tıklayarak tarayıcıda açabilirsiniz. 'Sayfalara Tıkla' tüm sonuçları otomatik ziyaret eder.",
            Location = new Point(15, 32),
            Size = new Size(810, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(127, 140, 141)
        };
        statusPanel.Controls.Add(lblTip);
        
        // Başlangıçta proxy listesini yükle
        _ = RefreshProxyList();
        
        // Başlangıç log mesajı
        Log("Sponsor Botu başlatıldı.", LogLevel.Info);
        Log($"Sistem: {Environment.OSVersion}", LogLevel.Debug);
        Log($".NET Version: {Environment.Version}", LogLevel.Debug);
    }
    
    // Log seviyeleri için enum
    private enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }
    
    // Log metodu
    private void Log(string message, LogLevel level = LogLevel.Info)
    {
        if (txtLog == null) return;
        
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => Log(message, level));
            return;
        }
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string prefix = level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            LogLevel.Success => "[OK]",
            _ => "[LOG]"
        };
        
        Color color = level switch
        {
            LogLevel.Debug => Color.Gray,
            LogLevel.Info => Color.FromArgb(0, 191, 255),
            LogLevel.Warning => Color.Orange,
            LogLevel.Error => Color.FromArgb(255, 80, 80),
            LogLevel.Success => Color.FromArgb(0, 255, 127),
            _ => Color.White
        };
        
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.SelectionColor = Color.DarkGray;
        txtLog.AppendText($"[{timestamp}] ");
        txtLog.SelectionColor = color;
        txtLog.AppendText($"{prefix} {message}\n");
        txtLog.ScrollToCaret();
    }
    
    private Button CreateStyledButton(string text, Point location, Size size, Color backColor)
    {
        var btn = new Button
        {
            Text = text,
            Location = location,
            Size = size,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor);
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor);
        return btn;
    }
    
    private async void LoadLogoAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            client.Timeout = TimeSpan.FromSeconds(10);
            var imageBytes = await client.GetByteArrayAsync("https://konyamobillastikci.com/img/resources/logo.png");
            using var ms = new MemoryStream(imageBytes);
            var originalImage = Image.FromStream(ms);
            picLogo.Image = originalImage;
        }
        catch
        {
            // Logo yüklenemezse placeholder göster
            picLogo.BackColor = primaryColor;
            var placeholder = new Label
            {
                Text = "SB",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            picLogo.Controls.Add(placeholder);
        }
    }

    private async Task RefreshProxyList()
    {
        const int targetWorkingProxies = 20;
        int attempt = 0;
        int maxAttempts = 10;
        
        try
        {
            workingProxyList.Clear();
            lblStatus.Text = $"20 çalışan proxy aranıyor...";
            lblStatus.ForeColor = Color.Blue;
            Log($"Hedef: {targetWorkingProxies} çalışan proxy bulmak.", LogLevel.Info);
            
            while (workingProxyList.Count < targetWorkingProxies && attempt < maxAttempts)
            {
                attempt++;
                Log($"Proxy arama turu {attempt}/{maxAttempts}...", LogLevel.Info);
                
                // Proxy'leri indir
                var newProxies = await Task.Run(() => FetchFreeProxies(attempt));
                
                if (newProxies.Count == 0)
                {
                    Log($"Tur {attempt}: Proxy indirilemedi.", LogLevel.Warning);
                    continue;
                }
                
                Log($"Tur {attempt}: {newProxies.Count} proxy indirildi. Test ediliyor...", LogLevel.Info);
                lblStatus.Text = $"Tur {attempt}: {newProxies.Count} proxy test ediliyor... (Şu an {workingProxyList.Count} çalışan)";
                
                // Proxy'leri test et
                int needed = targetWorkingProxies - workingProxyList.Count;
                var foundProxies = await TestProxiesUntilEnoughAsync(newProxies, needed);
                
                foreach (var proxy in foundProxies)
                {
                    if (!workingProxyList.Any(p => p.Host == proxy.Host && p.Port == proxy.Port))
                    {
                        workingProxyList.Add(proxy);
                    }
                }
                
                Log($"Tur {attempt} tamamlandı. Toplam çalışan proxy: {workingProxyList.Count}/{targetWorkingProxies}", LogLevel.Info);
                
                if (workingProxyList.Count >= targetWorkingProxies)
                {
                    break;
                }
                
                // Kısa bekleme
                await Task.Delay(500);
            }
            
            if (workingProxyList.Count >= targetWorkingProxies)
            {
                lblStatus.Text = $"✓ {workingProxyList.Count} çalışan proxy bulundu!";
                lblStatus.ForeColor = Color.Green;
                Log($"Hedef ulaşıldı! {workingProxyList.Count} çalışan proxy hazır.", LogLevel.Success);
            }
            else if (workingProxyList.Count > 0)
            {
                lblStatus.Text = $"⚠ {workingProxyList.Count} çalışan proxy bulundu (hedef: {targetWorkingProxies})";
                lblStatus.ForeColor = Color.Orange;
                Log($"{workingProxyList.Count} çalışan proxy bulundu (hedef: {targetWorkingProxies}). Mevcut proxy'lerle devam edilecek.", LogLevel.Warning);
            }
            else
            {
                lblStatus.Text = "Çalışan proxy bulunamadı, direkt bağlantı kullanılacak.";
                lblStatus.ForeColor = Color.Orange;
                Log("Hiçbir proxy çalışmıyor, direkt bağlantı kullanılacak.", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Proxy yüklenemedi, direkt bağlantı kullanılacak.";
            lblStatus.ForeColor = Color.Orange;
            Log($"Proxy yüklenirken hata: {ex.Message}", LogLevel.Error);
        }
    }
    
    private async Task<List<ProxyInfo>> TestProxiesUntilEnoughAsync(List<ProxyInfo> proxies, int needed)
    {
        var workingProxies = new System.Collections.Concurrent.ConcurrentBag<ProxyInfo>();
        int tested = 0;
        int total = proxies.Count;
        var cts = new CancellationTokenSource();
        
        var tasks = proxies.Select(async proxy =>
        {
            if (cts.Token.IsCancellationRequested) return;
            
            bool isWorking = await TestProxyAsync(proxy);
            Interlocked.Increment(ref tested);
            
            if (isWorking)
            {
                workingProxies.Add(proxy);
                Log($"✓ Proxy çalışıyor: {proxy.Host}:{proxy.Port} ({workingProxies.Count} bulundu)", LogLevel.Success);
                
                // Yeterli proxy bulundu, diğerlerini iptal et
                if (workingProxies.Count >= needed)
                {
                    cts.Cancel();
                }
            }
            
            // Her 10 test sonrası durum güncelle
            if (tested % 10 == 0)
            {
                try
                {
                    this.Invoke(() => 
                    {
                        lblStatus.Text = $"Test: {tested}/{total} ({workingProxyList.Count + workingProxies.Count} çalışan bulundu)";
                    });
                }
                catch { }
            }
        });
        
        try
        {
            await Task.WhenAll(tasks);
        }
        catch { }
        
        return workingProxies.ToList();
    }
    
    private async Task<List<ProxyInfo>> TestProxiesAsync(List<ProxyInfo> proxies)
    {
        return await TestProxiesUntilEnoughAsync(proxies, int.MaxValue);
    }
    
    private async Task<bool> TestProxyAsync(ProxyInfo proxy)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.Host}:{proxy.Port}"),
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            // Önce HTTP test et
            var httpResponse = await client.GetAsync("http://httpbin.org/ip");
            if (!httpResponse.IsSuccessStatusCode) return false;
            
            // Sonra HTTPS (CONNECT tunnel) test et - bu gerçek HTTPS sitelerinde çalışıp çalışmayacağını gösterir
            var httpsResponse = await client.GetAsync("https://httpbin.org/ip");
            if (!httpsResponse.IsSuccessStatusCode) return false;
            
            // Son olarak Google'a da bağlanmayı test et
            var googleResponse = await client.GetAsync("https://www.google.com");
            return googleResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<ProxyInfo> FetchFreeProxies(int round = 1)
    {
        var proxies = new List<ProxyInfo>();
        int skipCount = (round - 1) * 30; // Her turda farklı proxy'ler al
        
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            // Birden fazla ücretsiz proxy kaynağı
            var sources = new[]
            {
                "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=elite",
                "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
                "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/http.txt",
                "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
                "https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list-raw.txt",
                "https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies-http.txt",
                "https://raw.githubusercontent.com/mmpx12/proxy-list/master/http.txt",
                "https://raw.githubusercontent.com/roosterkid/openproxylist/main/HTTPS_RAW.txt"
            };
            
            foreach (var source in sources)
            {
                try
                {
                    var response = client.GetStringAsync(source).Result;
                    var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Her turda farklı proxy'leri al
                    var selectedLines = lines.Skip(skipCount).Take(20);
                    
                    foreach (var line in selectedLines)
                    {
                        var parts = line.Trim().Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                        {
                            var proxyInfo = new ProxyInfo { Host = parts[0], Port = port };
                            // Daha önce eklenmemişse ekle
                            if (!proxies.Any(p => p.Host == proxyInfo.Host && p.Port == proxyInfo.Port) &&
                                !workingProxyList.Any(p => p.Host == proxyInfo.Host && p.Port == proxyInfo.Port))
                            {
                                proxies.Add(proxyInfo);
                            }
                        }
                    }
                }
                catch { }
                
                if (proxies.Count >= 100) break;
            }
        }
        catch { }
        
        // Proxy'leri karıştır
        return proxies.OrderBy(x => random.Next()).ToList();
    }

    private ProxyInfo? GetNextProxy()
    {
        // Önce çalışan proxy listesini kullan
        if (workingProxyList.Count > 0)
        {
            currentProxyIndex = (currentProxyIndex + 1) % workingProxyList.Count;
            return workingProxyList[currentProxyIndex];
        }
        
        // Çalışan proxy yoksa normal listeden dene
        if (proxyList.Count == 0) return null;
        
        currentProxyIndex = (currentProxyIndex + 1) % proxyList.Count;
        return proxyList[currentProxyIndex];
    }
    
    private void RemoveFailedProxy(ProxyInfo proxy)
    {
        workingProxyList.Remove(proxy);
        Log($"Başarısız proxy listeden çıkarıldı: {proxy.Host}:{proxy.Port} (Kalan: {workingProxyList.Count})", LogLevel.Warning);
    }

    private void TxtKeyword_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.Enter)
        {
            e.SuppressKeyPress = true;
            BtnSearch_Click(sender, e);
        }
    }

    private async void BtnSearch_Click(object? sender, EventArgs e)
    {
        string keyword = txtKeyword.Text.Trim();
        int resultCount = (int)numResultCount.Value;

        if (string.IsNullOrEmpty(keyword))
        {
            MessageBox.Show("Lütfen bir anahtar kelime girin!", "Uyarı", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Log("Arama başlatılamadı: Anahtar kelime boş.", LogLevel.Warning);
            return;
        }

        lstResults.Items.Clear();
        currentResults.Clear();
        btnSearch.Enabled = false;
        btnClickPages.Enabled = false;
        progressBar.Visible = true;
        lblStatus.Text = "Chrome başlatılıyor ve Google'da arama yapılıyor...";
        lblStatus.ForeColor = Color.Blue;
        
        Log($"Arama başlatıldı: '{keyword}' (Max {resultCount} sonuç)", LogLevel.Info);
        Log($"Tarayıcı görünür: {chkShowBrowser.Checked}", LogLevel.Debug);

        try
        {
            var results = await Task.Run(() => SearchGoogleWithSelenium(keyword, chkShowBrowser.Checked, resultCount));
            currentResults = results;

            if (results.Count == 0)
            {
                lblStatus.Text = "Sonuç bulunamadı.";
                lblStatus.ForeColor = Color.Orange;
                Log("Arama tamamlandı ancak sonuç bulunamadı.", LogLevel.Warning);
            }
            else
            {
                int sponsorCount = results.Count(r => r.IsSponsored);
                int organicCount = results.Count - sponsorCount;
                
                foreach (var result in results)
                {
                    string icon = result.IsSponsored ? "💰" : "📌";
                    lstResults.Items.Add($"{icon} {result.Title}");
                    lstResults.Items.Add($"   🔗 {result.Url}");
                    lstResults.Items.Add(""); // Boş satır
                    
                    Log($"Sonuç bulundu: {result.Title} ({(result.IsSponsored ? "Sponsorlu" : "Organik")})", LogLevel.Debug);
                }
                
                string statusText = $"{results.Count} sonuç bulundu";
                if (sponsorCount > 0)
                {
                    statusText += $" ({sponsorCount} sponsorlu, {organicCount} organik)";
                }
                lblStatus.Text = statusText;
                lblStatus.ForeColor = Color.Green;
                btnClickPages.Enabled = true;
                
                Log($"Arama tamamlandı: {results.Count} sonuç ({sponsorCount} sponsorlu, {organicCount} organik)", LogLevel.Success);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Hata: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            Log($"Arama hatası: {ex.Message}", LogLevel.Error);
            Log($"Stack Trace: {ex.StackTrace}", LogLevel.Debug);
            MessageBox.Show($"Arama sırasında bir hata oluştu:\n{ex.Message}\n\nChrome yüklü olduğundan emin olun.", "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSearch.Enabled = true;
            progressBar.Visible = false;
        }
    }

    private async void BtnClickPages_Click(object? sender, EventArgs e)
    {
        if (currentResults.Count == 0)
        {
            MessageBox.Show("Önce arama yapın!", "Uyarı", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Log("Sayfa tıklama başlatılamadı: Sonuç listesi boş.", LogLevel.Warning);
            return;
        }

        int loopCount = (int)numLoopCount.Value;
        stopRequested = false;
        cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        
        btnSearch.Enabled = false;
        btnClickPages.Enabled = false;
        btnStop.Enabled = true;
        progressBar.Visible = true;
        
        Log($"Sayfa tıklama başlatıldı: {currentResults.Count} sayfa x {loopCount} döngü", LogLevel.Info);
        Log($"IP Rotasyonu: {(chkRotateIP.Checked ? "Aktif" : "Pasif")}", LogLevel.Debug);

        try
        {
            for (int loop = 1; loop <= loopCount; loop++)
            {
                if (stopRequested || cancellationToken.IsCancellationRequested) break;
                
                lblStatus.Text = $"Döngü {loop}/{loopCount} - Sayfalara tıklanıyor...";
                lblStatus.ForeColor = Color.Blue;
                Log($"Döngü {loop}/{loopCount} başlatıldı.", LogLevel.Info);
                
                await Task.Run(() => ClickAllPages(currentResults, loop, loopCount, cancellationToken), cancellationToken);
                
                if (stopRequested || cancellationToken.IsCancellationRequested) break;
                
                // Döngüler arası bekleme
                if (loop < loopCount)
                {
                    lblStatus.Text = $"Döngü {loop}/{loopCount} tamamlandı. Sonraki döngü için bekleniyor...";
                    Log($"Döngü {loop} tamamlandı. Sonraki döngü için bekleniyor...", LogLevel.Info);
                    try { await Task.Delay(random.Next(2000, 5000), cancellationToken); } catch (OperationCanceledException) { break; }
                }
            }
            
            if (stopRequested || (cancellationTokenSource?.IsCancellationRequested ?? false))
            {
                lblStatus.Text = "⏹ İşlem durduruldu.";
                lblStatus.ForeColor = Color.Orange;
                Log("⏹ İşlem kullanıcı tarafından durduruldu.", LogLevel.Warning);
            }
            else
            {
                lblStatus.Text = $"{loopCount} döngü tamamlandı! Toplam {currentResults.Count * loopCount} sayfa ziyaret edildi.";
                lblStatus.ForeColor = Color.Green;
                Log($"Tüm döngüler tamamlandı! Toplam {currentResults.Count * loopCount} sayfa ziyaret edildi.", LogLevel.Success);
            }
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "⏹ İşlem durduruldu.";
            lblStatus.ForeColor = Color.Orange;
            Log("⏹ İşlem iptal edildi.", LogLevel.Warning);
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Hata: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            Log($"Sayfa tıklama hatası: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            btnSearch.Enabled = true;
            btnClickPages.Enabled = true;
            btnStop.Enabled = false;
            progressBar.Visible = false;
            stopRequested = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        stopRequested = true;
        cancellationTokenSource?.Cancel();
        lblStatus.Text = "⚠️ Durduruluyor...";
        lblStatus.ForeColor = Color.Orange;
        btnStop.Enabled = false;
        Log("⚠️ Durdurma isteği gönderildi - işlem iptal ediliyor...", LogLevel.Warning);
    }

    private void ClickAllPages(List<SearchResult> results, int currentLoop, int totalLoops, CancellationToken cancellationToken)
    {
        int pageIndex = 0;
        foreach (var result in results)
        {
            if (stopRequested || cancellationToken.IsCancellationRequested) break;
            
            pageIndex++;
            Log($"[Döngü {currentLoop}/{totalLoops}] Sayfa {pageIndex}/{results.Count} ziyaret ediliyor: {result.Url}", LogLevel.Info);
            
            bool success = false;
            int maxRetries = 3;
            ProxyInfo? currentProxy = null;
            
            for (int retry = 0; retry < maxRetries && !success && !stopRequested && !cancellationToken.IsCancellationRequested; retry++)
            {
                if (retry > 0)
                {
                    Log($"Yeniden deneniyor... (Deneme {retry + 1}/{maxRetries})", LogLevel.Warning);
                }
                
                // Her sayfa için yeni proxy ile yeni tarayıcı aç
                currentProxy = chkRotateIP.Checked ? GetNextProxy() : null;
                var options = CreateHumanLikeOptionsWithProxy(currentProxy);
                
                var service = ChromeDriverService.CreateDefaultService();
                service.SuppressInitialDiagnosticInformation = true;
                service.HideCommandPromptWindow = true;

                ChromeDriver? driver = null;
                
                try
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    if (currentProxy != null)
                    {
                        Log($"Proxy kullanılıyor: {currentProxy.Host}:{currentProxy.Port}", LogLevel.Debug);
                    }
                    else
                    {
                        Log("Direkt bağlantı kullanılıyor (proxy yok).", LogLevel.Debug);
                    }
                    
                    Log("Chrome başlatılıyor...", LogLevel.Debug);
                    driver = new ChromeDriver(service, options);
                    
                    // WebDriver tespitini engellemek için JavaScript enjekte et
                    InjectAntiDetectionScripts(driver);
                    Log("Anti-detection scriptleri enjekte edildi.", LogLevel.Debug);
                    
                    // Rastgele bekleme (insan gibi) - iptal edilebilir
                    if (cancellationToken.IsCancellationRequested) { driver?.Quit(); break; }
                    Thread.Sleep(Math.Min(random.Next(500, 1500), 500));
                    
                    if (stopRequested || cancellationToken.IsCancellationRequested) { driver?.Quit(); break; }
                    
                    // Önce Google'dan gel gibi yap (referrer için)
                    driver.Navigate().GoToUrl("https://www.google.com");
                    Log("Google referrer sayfası yüklendi.", LogLevel.Debug);
                    
                    if (cancellationToken.IsCancellationRequested) { driver?.Quit(); break; }
                    Thread.Sleep(Math.Min(random.Next(1000, 2000), 500));
                    
                    if (stopRequested || cancellationToken.IsCancellationRequested) { driver?.Quit(); break; }
                    
                    // Şimdi hedef siteye git
                    driver.Navigate().GoToUrl(result.Url);
                    Log($"Hedef sayfa yükleniyor: {result.Url}", LogLevel.Debug);
                    
                    // Sayfanın tamamen yüklenmesini bekle
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");
                    Log("Sayfa tamamen yüklendi.", LogLevel.Debug);
                    
                    if (cancellationToken.IsCancellationRequested) { driver?.Quit(); break; }
                    
                    // Anti-detection scriptlerini tekrar enjekte et
                    InjectAntiDetectionScripts(driver);
                    
                    // İnsan davranışını simüle et
                    SimulateHumanBehavior(driver);
                    Log("İnsan davranışı simüle edildi (scroll, mouse hareketi).", LogLevel.Debug);
                    
                    // Kısa bekleme - iptal edilebilir
                    if (!cancellationToken.IsCancellationRequested)
                        Thread.Sleep(Math.Min(random.Next(2000, 4000), 1000));
                    
                    success = true;
                    Log($"✓ Sayfa {pageIndex} başarıyla ziyaret edildi.", LogLevel.Success);
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Log("İşlem iptal edildi.", LogLevel.Warning);
                    }
                    else
                    {
                        string errorMsg = ex.Message;
                        
                        // Proxy hatası mı kontrol et
                        if (errorMsg.Contains("ERR_TUNNEL_CONNECTION_FAILED") || 
                            errorMsg.Contains("ERR_PROXY_CONNECTION_FAILED") ||
                            errorMsg.Contains("ERR_CONNECTION_REFUSED") ||
                            errorMsg.Contains("ERR_CONNECTION_TIMED_OUT"))
                        {
                            if (currentProxy != null)
                            {
                                RemoveFailedProxy(currentProxy);
                            }
                            Log($"Proxy bağlantı hatası: {errorMsg}", LogLevel.Error);
                        }
                        else
                        {
                            Log($"Sayfa hatası: {errorMsg}", LogLevel.Error);
                        }
                    }
                }
                finally
                {
                    try { driver?.Quit(); } catch { }
                }
                
                if (!success && retry < maxRetries - 1 && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(500);
                }
            }
            
            if (!success && !cancellationToken.IsCancellationRequested)
            {
                Log($"✗ Sayfa {pageIndex} tüm denemelerde başarısız oldu.", LogLevel.Error);
            }
            
            if (stopRequested || cancellationToken.IsCancellationRequested) break;
            
            // Sayfalar arası kısa bekleme
            if (!cancellationToken.IsCancellationRequested)
                Thread.Sleep(Math.Min(random.Next(1000, 2000), 500));
        }
    }
    
    private ChromeOptions CreateHumanLikeOptionsWithProxy(ProxyInfo? proxy)
    {
        var options = new ChromeOptions();
        
        // Proxy kullan
        if (proxy != null)
        {
            options.AddArgument($"--proxy-server=http://{proxy.Host}:{proxy.Port}");
        }
        
        bool isMobile = rdoMobile.Checked;
        string resolution;
        string userAgent;
        
        if (isMobile)
        {
            // Mobil ekran çözünürlükleri
            var mobileResolutions = new[] { "375,812", "390,844", "414,896", "360,740", "412,915", "393,873", "428,926" };
            resolution = mobileResolutions[random.Next(mobileResolutions.Length)];
            
            // Mobil User-Agent'ları (iPhone ve Android)
            var mobileUserAgents = new[]
            {
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/131.0.6778.73 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 14; Pixel 8 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 13; SM-A546B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 14; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36"
            };
            userAgent = mobileUserAgents[random.Next(mobileUserAgents.Length)];
        }
        else
        {
            // Masaüstü ekran çözünürlükleri
            var desktopResolutions = new[] { "1920,1080", "1366,768", "1536,864", "1440,900", "1280,720", "1600,900", "2560,1440" };
            resolution = desktopResolutions[random.Next(desktopResolutions.Length)];
            
            // Masaüstü User-Agent'ları
            var desktopUserAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 OPR/115.0.0.0",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
            };
            userAgent = desktopUserAgents[random.Next(desktopUserAgents.Length)];
        }
        
        // Anti-bot ayarları
        options.AddArgument($"--window-size={resolution}");
        options.AddArgument($"user-agent={userAgent}");
        options.AddArgument("--lang=tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--log-level=3");
        options.AddArgument("--disable-infobars");
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--allow-running-insecure-content");
        options.AddArgument("--headless=new"); // Arka planda çalış
        
        // WebRTC IP sızıntısını engelle
        options.AddArgument("--disable-webrtc");
        
        // Otomasyon bayrağını gizle
        options.AddAdditionalOption("useAutomationExtension", false);
        
        // Gerçek tarayıcı gibi görünmek için ek tercihler
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
        options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 2);
        options.AddUserProfilePreference("webrtc.ip_handling_policy", "disable_non_proxied_udp");
        options.AddUserProfilePreference("webrtc.multiple_routes_enabled", false);
        options.AddUserProfilePreference("webrtc.nonproxied_udp_enabled", false);
        
        return options;
    }

    private ChromeOptions CreateHumanLikeOptions(bool useProxy = false)
    {
        var options = new ChromeOptions();
        
        // Proxy kullan (arama için genelde proxy kullanılmaz)
        if (useProxy && chkRotateIP.Checked)
        {
            var proxy = GetNextProxy();
            if (proxy != null)
            {
                options.AddArgument($"--proxy-server=http://{proxy.Host}:{proxy.Port}");
            }
        }
        
        bool isMobile = rdoMobile.Checked;
        string resolution;
        string userAgent;
        
        if (isMobile)
        {
            // Mobil ekran çözünürlükleri
            var mobileResolutions = new[] { "375,812", "390,844", "414,896", "360,740", "412,915", "393,873", "428,926" };
            resolution = mobileResolutions[random.Next(mobileResolutions.Length)];
            
            // Mobil User-Agent'ları (iPhone ve Android)
            var mobileUserAgents = new[]
            {
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/131.0.6778.73 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 14; Pixel 8 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 13; SM-A546B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 14; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36"
            };
            userAgent = mobileUserAgents[random.Next(mobileUserAgents.Length)];
        }
        else
        {
            // Masaüstü ekran çözünürlükleri
            var desktopResolutions = new[] { "1920,1080", "1366,768", "1536,864", "1440,900", "1280,720", "1600,900", "2560,1440" };
            resolution = desktopResolutions[random.Next(desktopResolutions.Length)];
            
            // Masaüstü User-Agent'ları
            var desktopUserAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 OPR/115.0.0.0",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
            };
            userAgent = desktopUserAgents[random.Next(desktopUserAgents.Length)];
        }
        
        // Anti-bot ayarları
        options.AddArgument($"--window-size={resolution}");
        options.AddArgument($"user-agent={userAgent}");
        options.AddArgument("--lang=tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--log-level=3");
        options.AddArgument("--disable-infobars");
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--allow-running-insecure-content");
        
        // WebRTC IP sızıntısını engelle
        options.AddArgument("--disable-webrtc");
        
        // Otomasyon bayrağını gizle
        options.AddAdditionalOption("useAutomationExtension", false);
        
        // Gerçek tarayıcı gibi görünmek için ek tercihler
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
        options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 2);
        options.AddUserProfilePreference("webrtc.ip_handling_policy", "disable_non_proxied_udp");
        options.AddUserProfilePreference("webrtc.multiple_routes_enabled", false);
        options.AddUserProfilePreference("webrtc.nonproxied_udp_enabled", false);
        
        return options;
    }

    private void InjectAntiDetectionScripts(ChromeDriver driver)
    {
        try
        {
            var js = (IJavaScriptExecutor)driver;
            
            // WebDriver özelliğini gizle
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined
                });
            ");
            
            // Chrome özelliklerini gizle
            js.ExecuteScript(@"
                window.chrome = {
                    runtime: {
                        connect: function() {},
                        sendMessage: function() {}
                    },
                    loadTimes: function() {
                        return {
                            commitLoadTime: Date.now() / 1000 - Math.random() * 2,
                            connectionInfo: 'h2',
                            finishDocumentLoadTime: Date.now() / 1000 - Math.random(),
                            finishLoadTime: Date.now() / 1000 - Math.random() * 0.5,
                            firstPaintAfterLoadTime: 0,
                            firstPaintTime: Date.now() / 1000 - Math.random() * 3,
                            navigationType: 'Other',
                            npnNegotiatedProtocol: 'h2',
                            requestTime: Date.now() / 1000 - Math.random() * 4,
                            startLoadTime: Date.now() / 1000 - Math.random() * 3.5,
                            wasAlternateProtocolAvailable: false,
                            wasFetchedViaSpdy: true,
                            wasNpnNegotiated: true
                        };
                    },
                    csi: function() {
                        return {
                            onloadT: Date.now(),
                            pageT: Math.random() * 1000 + 500,
                            startE: Date.now() - Math.random() * 5000,
                            tran: 15
                        };
                    },
                    app: {
                        isInstalled: false,
                        InstallState: { DISABLED: 'disabled', INSTALLED: 'installed', NOT_INSTALLED: 'not_installed' },
                        RunningState: { CANNOT_RUN: 'cannot_run', READY_TO_RUN: 'ready_to_run', RUNNING: 'running' }
                    }
                };
            ");
            
            // Plugins'i gerçekçi yap
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'plugins', {
                    get: () => {
                        const plugins = [
                            { name: 'Chrome PDF Plugin', filename: 'internal-pdf-viewer', description: 'Portable Document Format' },
                            { name: 'Chrome PDF Viewer', filename: 'mhjfbmdgcfjbbpaeojofohoefgiehjai', description: '' },
                            { name: 'Native Client', filename: 'internal-nacl-plugin', description: '' },
                            { name: 'Chromium PDF Plugin', filename: 'internal-pdf-viewer', description: 'Portable Document Format' }
                        ];
                        plugins.item = (i) => plugins[i];
                        plugins.namedItem = (name) => plugins.find(p => p.name === name);
                        plugins.refresh = () => {};
                        return plugins;
                    }
                });
            ");
            
            // MimeTypes gerçekçi yap
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'mimeTypes', {
                    get: () => {
                        const mimeTypes = [
                            { type: 'application/pdf', suffixes: 'pdf', description: 'Portable Document Format' },
                            { type: 'text/pdf', suffixes: 'pdf', description: 'Portable Document Format' }
                        ];
                        mimeTypes.item = (i) => mimeTypes[i];
                        mimeTypes.namedItem = (name) => mimeTypes.find(m => m.type === name);
                        return mimeTypes;
                    }
                });
            ");
            
            // Languages'i gerçekçi yap
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'languages', {
                    get: () => ['tr-TR', 'tr', 'en-US', 'en']
                });
                Object.defineProperty(navigator, 'language', {
                    get: () => 'tr-TR'
                });
            ");
            
            // Platform
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'platform', {
                    get: () => 'Win32'
                });
            ");
            
            // Hardware Concurrency (CPU çekirdek sayısı)
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'hardwareConcurrency', {
                    get: () => " + (random.Next(4, 16)) + @"
                });
            ");
            
            // Device Memory
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'deviceMemory', {
                    get: () => " + (new[] { 4, 8, 16, 32 }[random.Next(4)]) + @"
                });
            ");
            
            // Connection bilgisi (Google Analytics bunu kontrol eder)
            js.ExecuteScript(@"
                Object.defineProperty(navigator, 'connection', {
                    get: () => ({
                        downlink: " + (random.Next(10, 100)) + @",
                        effectiveType: '4g',
                        rtt: " + (random.Next(50, 200)) + @",
                        saveData: false,
                        onchange: null
                    })
                });
            ");
            
            // Permissions API'yi gizle
            js.ExecuteScript(@"
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                );
            ");
            
            // WebGL bilgilerini gerçekçi yap (fingerprinting koruması)
            js.ExecuteScript(@"
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) {
                        return 'Intel Inc.';
                    }
                    if (parameter === 37446) {
                        return 'Intel Iris OpenGL Engine';
                    }
                    return getParameter.apply(this, arguments);
                };
            ");
            
            // Canvas fingerprinting koruması
            js.ExecuteScript(@"
                const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
                HTMLCanvasElement.prototype.toDataURL = function(type) {
                    if (type === 'image/png' && this.width > 16 && this.height > 16) {
                        const context = this.getContext('2d');
                        const imageData = context.getImageData(0, 0, this.width, this.height);
                        for (let i = 0; i < imageData.data.length; i += 4) {
                            imageData.data[i] = imageData.data[i] ^ (Math.random() > 0.5 ? 1 : 0);
                        }
                        context.putImageData(imageData, 0, 0);
                    }
                    return originalToDataURL.apply(this, arguments);
                };
            ");
            
            // AudioContext fingerprinting koruması
            js.ExecuteScript(@"
                const originalGetChannelData = AudioBuffer.prototype.getChannelData;
                AudioBuffer.prototype.getChannelData = function(channel) {
                    const array = originalGetChannelData.apply(this, arguments);
                    for (let i = 0; i < array.length; i += 100) {
                        array[i] = array[i] + Math.random() * 0.0001;
                    }
                    return array;
                };
            ");
            
            // Google Analytics'i engelle veya kandır
            js.ExecuteScript(@"
                // GA scriptlerini engelle
                const originalCreateElement = document.createElement;
                document.createElement = function(tagName) {
                    const element = originalCreateElement.call(document, tagName);
                    if (tagName.toLowerCase() === 'script') {
                        const originalSetAttribute = element.setAttribute;
                        element.setAttribute = function(name, value) {
                            if (name === 'src' && (value.includes('google-analytics') || value.includes('googletagmanager') || value.includes('gtag'))) {
                                return;
                            }
                            return originalSetAttribute.apply(this, arguments);
                        };
                    }
                    return element;
                };
                
                // Sahte GA objesi oluştur
                window.ga = function() {};
                window.ga.create = function() {};
                window.ga.send = function() {};
                window.ga.set = function() {};
                window.gtag = function() {};
                window.dataLayer = [];
            ");
            
            // Timestamp'leri gerçekçi yap
            js.ExecuteScript(@"
                const originalNow = Date.now;
                const offset = Math.floor(Math.random() * 1000);
                Date.now = function() {
                    return originalNow() + offset;
                };
            ");
            
            // Battery API (eğer varsa)
            js.ExecuteScript(@"
                if (navigator.getBattery) {
                    navigator.getBattery = () => Promise.resolve({
                        charging: true,
                        chargingTime: 0,
                        dischargingTime: Infinity,
                        level: 0.85 + Math.random() * 0.15,
                        onchargingchange: null,
                        onchargingtimechange: null,
                        ondischargingtimechange: null,
                        onlevelchange: null
                    });
                }
            ");
        }
        catch { }
    }

    private void SimulateHumanBehavior(ChromeDriver driver)
    {
        try
        {
            var actions = new Actions(driver);
            var js = (IJavaScriptExecutor)driver;
            
            // Sayfa boyutunu al
            long pageHeight = (long)js.ExecuteScript("return document.body.scrollHeight");
            long viewportHeight = (long)js.ExecuteScript("return window.innerHeight");
            
            // Rastgele scroll yap (insan gibi)
            int scrollCount = random.Next(2, 5);
            for (int i = 0; i < scrollCount; i++)
            {
                int scrollAmount = random.Next(100, 400);
                js.ExecuteScript($"window.scrollBy({{ top: {scrollAmount}, behavior: 'smooth' }});");
                Thread.Sleep(random.Next(300, 800));
            }
            
            // Rastgele mouse hareketi
            try
            {
                int moveX = random.Next(100, 800);
                int moveY = random.Next(100, 500);
                actions.MoveByOffset(moveX, moveY).Perform();
                Thread.Sleep(random.Next(100, 300));
                
                // Bazen geri git
                moveX = random.Next(-200, 200);
                moveY = random.Next(-100, 100);
                actions.MoveByOffset(moveX, moveY).Perform();
            }
            catch { }
            
            // Bazen sayfada bir yere tıkla (boş alana)
            if (random.Next(100) < 30) // %30 ihtimal
            {
                try
                {
                    var body = driver.FindElement(By.TagName("body"));
                    actions.MoveToElement(body, random.Next(100, 500), random.Next(100, 300)).Click().Perform();
                }
                catch { }
            }
            
            // Yukarı scroll
            Thread.Sleep(random.Next(500, 1000));
            js.ExecuteScript("window.scrollTo({ top: 0, behavior: 'smooth' });");
            Thread.Sleep(random.Next(300, 600));
        }
        catch { }
    }

    private List<SearchResult> SearchGoogleWithSelenium(string keyword, bool showBrowser, int maxResults)
    {
        var results = new List<SearchResult>();
        
        var options = CreateHumanLikeOptions(useProxy: false); // Arama için proxy kullanma
        
        if (!showBrowser)
        {
            options.AddArgument("--headless=new");
        }
        
        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;

        using var driver = new ChromeDriver(service, options);
        
        // Anti-detection scriptlerini enjekte et
        InjectAntiDetectionScripts(driver);
        
        try
        {
            // Google'a git
            string searchUrl = $"https://www.google.com/search?q={WebUtility.UrlEncode(keyword)}&hl=tr&num={maxResults + 10}";
            driver.Navigate().GoToUrl(searchUrl);
            
            // Sayfanın yüklenmesini bekle
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            wait.Until(d => d.FindElements(By.CssSelector("div#search, div#rso")).Count > 0);
            
            // Anti-detection scriptlerini tekrar enjekte et
            InjectAntiDetectionScripts(driver);
            
            // İnsan gibi bekle
            Thread.Sleep(random.Next(500, 1000));
            
            // Cookie popup'ı kapat (varsa)
            try
            {
                var rejectButton = driver.FindElements(By.XPath("//button[contains(., 'Tümünü reddet') or contains(., 'Reject all')]"));
                if (rejectButton.Count > 0)
                {
                    rejectButton[0].Click();
                    Thread.Sleep(random.Next(300, 700));
                }
            }
            catch { }

            // 1. Önce sponsorlu reklamları çek
            try
            {
                // Sponsorlu reklamlar genelde üstte "Sponsorlu" etiketi ile görünür
                var adContainers = driver.FindElements(By.CssSelector("div[data-text-ad], div.uEierd, div[data-hveid] div.commercial-unit-desktop-top"));
                
                foreach (var adContainer in adContainers)
                {
                    try
                    {
                        var linkElement = adContainer.FindElement(By.CssSelector("a[href^='http']"));
                        var titleElement = adContainer.FindElements(By.CssSelector("div[role='heading'], h3, span.cfxYMc")).FirstOrDefault();
                        
                        string url = linkElement.GetAttribute("href") ?? "";
                        string title = titleElement?.Text ?? linkElement.Text ?? "";
                        
                        if (!string.IsNullOrEmpty(url) && !url.Contains("google.com") && 
                            !string.IsNullOrEmpty(title) && title.Length > 3)
                        {
                            if (!results.Any(r => r.Url == url))
                            {
                                results.Add(new SearchResult
                                {
                                    Title = "[SPONSORLU] " + title,
                                    Url = url,
                                    IsSponsored = true
                                });
                            }
                        }
                    }
                    catch { }
                    
                    if (results.Count >= maxResults) break;
                }
            }
            catch { }

            // 2. Alternatif: "Sponsorlu" yazısı olan bölümleri ara
            if (results.Count < maxResults)
            {
                try
                {
                    var sponsoredLabels = driver.FindElements(By.XPath("//*[contains(text(), 'Sponsorlu') or contains(text(), 'Sponsored') or contains(text(), 'Ad')]"));
                    
                    foreach (var label in sponsoredLabels)
                    {
                        try
                        {
                            // Parent containerı bul
                            var container = label.FindElement(By.XPath("ancestor::div[.//a[@href]]"));
                            var links = container.FindElements(By.CssSelector("a[href^='http']"));
                            
                            foreach (var link in links)
                            {
                                string url = link.GetAttribute("href") ?? "";
                                if (!string.IsNullOrEmpty(url) && !url.Contains("google.com") && !url.Contains("googleads"))
                                {
                                    var titleEl = container.FindElements(By.CssSelector("h3, div[role='heading']")).FirstOrDefault();
                                    string title = titleEl?.Text ?? link.Text ?? "";
                                    
                                    if (!string.IsNullOrEmpty(title) && title.Length > 3)
                                    {
                                        if (!results.Any(r => r.Url == url))
                                        {
                                            results.Add(new SearchResult
                                            {
                                                Title = "[SPONSORLU] " + title,
                                                Url = url,
                                                IsSponsored = true
                                            });
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        
                        if (results.Count >= maxResults) break;
                    }
                }
                catch { }
            }

            // 3. Organik sonuçları çek (eğer yeterli sponsorlu yoksa)
            if (results.Count < maxResults)
            {
                try
                {
                    var organicResults = driver.FindElements(By.CssSelector("div#rso div.g, div#search div.g"));
                    
                    foreach (var result in organicResults)
                    {
                        try
                        {
                            var linkElement = result.FindElement(By.CssSelector("a[href^='http']"));
                            var titleElement = result.FindElement(By.CssSelector("h3"));
                            
                            string url = linkElement.GetAttribute("href") ?? "";
                            string title = titleElement.Text ?? "";
                            
                            if (!string.IsNullOrEmpty(url) && !url.Contains("google.com") && 
                                !string.IsNullOrEmpty(title))
                            {
                                if (!results.Any(r => r.Url == url))
                                {
                                    results.Add(new SearchResult
                                    {
                                        Title = title,
                                        Url = url,
                                        IsSponsored = false
                                    });
                                }
                            }
                        }
                        catch { }
                        
                        if (results.Count >= maxResults) break;
                    }
                }
                catch { }
            }
        }
        finally
        {
            driver.Quit();
        }

        return results;
    }

    private void LstResults_DoubleClick(object? sender, EventArgs e)
    {
        if (lstResults.SelectedItem != null)
        {
            string selectedText = lstResults.SelectedItem.ToString() ?? "";
            
            // URL satırını bul
            if (selectedText.Contains("🔗"))
            {
                string url = selectedText.Replace("   🔗 ", "").Trim();
                OpenUrl(url);
            }
            else if (selectedText.Contains("💰") || selectedText.Contains("📌"))
            {
                // Başlık satırına tıklandıysa, bir sonraki satırdaki URL'yi al
                int index = lstResults.SelectedIndex;
                if (index + 1 < lstResults.Items.Count)
                {
                    string urlLine = lstResults.Items[index + 1]?.ToString() ?? "";
                    if (urlLine.Contains("🔗"))
                    {
                        string url = urlLine.Replace("   🔗 ", "").Trim();
                        OpenUrl(url);
                    }
                }
            }
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"URL açılırken hata oluştu:\n{ex.Message}", "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CheckForUpdatesManually()
    {
        Log("Manuel güncelleme kontrolü başlatıldı...", LogLevel.Info);
        lblStatus.Text = "🔄 Güncelleme kontrol ediliyor...";
        lblStatus.ForeColor = Color.Blue;
        
        try
        {
            var (hasUpdate, updateInfo) = await UpdateChecker.CheckForUpdateAsync();
            
            if (hasUpdate && updateInfo != null)
            {
                Log($"Yeni güncelleme bulundu: v{updateInfo.Version}", LogLevel.Success);
                lblStatus.Text = $"Yeni güncelleme mevcut: v{updateInfo.Version}";
                lblStatus.ForeColor = Color.Green;
                
                // ShowUpdateDialog içinde otomatik indirme ve kurulum yapılıyor
                UpdateChecker.ShowUpdateDialog(updateInfo, this);
            }
            else
            {
                Log($"Uygulama güncel. Mevcut versiyon: v{UpdateChecker.CurrentVersion}", LogLevel.Info);
                lblStatus.Text = $"✅ Uygulama güncel (v{UpdateChecker.CurrentVersion})";
                lblStatus.ForeColor = Color.Green;
                
                MessageBox.Show($"Uygulamanız güncel!\n\nMevcut Versiyon: v{UpdateChecker.CurrentVersion}", 
                    "Güncelleme Kontrolü", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch
        {
            // Güncelleme kontrolü başarısız olursa sessizce devam et
            Log("Güncelleme kontrolü başarısız - bağlantı sorunu olabilir", LogLevel.Warning);
            lblStatus.Text = "Arama yapmak için bir anahtar kelime girin.";
            lblStatus.ForeColor = primaryColor;
        }
    }

    private void ShowInfoDialog()
    {
        var infoForm = new Form
        {
            Text = "Sponsor Botu - Bilgilendirme",
            Size = new Size(600, 500),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(236, 240, 241),
            Icon = SystemIcons.Information
        };

        var tabControlInfo = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(565, 400),
            Font = new Font("Segoe UI", 10)
        };
        infoForm.Controls.Add(tabControlInfo);

        // ===== NASIL KULLANILIR TAB =====
        var tabHowTo = new TabPage
        {
            Text = "📖 Nasıl Kullanılır",
            BackColor = Color.White,
            Padding = new Padding(15)
        };
        tabControlInfo.TabPages.Add(tabHowTo);

        var txtHowTo = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(44, 62, 80),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        txtHowTo.Text = @"🚀 SPONSOR BOTU KULLANIM KILAVUZU
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📌 TEMEL KULLANIM
1. Anahtar kelime kutusuna aramak istediğiniz kelimeyi yazın
2. 'Sonuç' sayısını belirleyin (1-20 arası)
3. 'Google'da Ara' butonuna tıklayın
4. Sonuçlar listelenince 'Sayfalara Tıkla' butonuyla ziyaret edin

⚙️ SEÇENEKLER

🖥️ Göster: Tarayıcı penceresini görünür yapar
   (İşlemleri izlemek için açabilirsiniz)

🔄 Proxy: IP rotasyonu için proxy kullanır
   (Tespit edilmemek için önerilir)

💻 Masaüstü / 📱 Mobil: Cihaz tipini seçin
   - Masaüstü: Bilgisayar gibi arama yapar
   - Mobil: Telefon gibi arama yapar (mobil reklamlar)

🔁 Döngü: İşlemi kaç kez tekrarlayacağını belirler

🔄 Proxy Yenile: Yeni çalışan proxy'ler bulur
   (20 çalışan proxy bulunana kadar arar)

⏹ Durdur: Çalışan işlemi durdurur

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

� GÜNCELLEME SİSTEMİ
• Uygulama her açılışta otomatik güncelleme kontrolü yapar
• Sağ üstteki 🔄 butonuyla manuel kontrol yapabilirsiniz
• Güncellemeler tüm cihazlarınıza otomatik gelir

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

�💡 İPUÇLARI
• Sonuçlara çift tıklayarak tarayıcıda açabilirsiniz
• Log sekmesinden tüm işlemleri takip edebilirsiniz
• Proxy'ler otomatik test edilir, sadece çalışanlar kullanılır
• Her döngüde farklı IP ve User-Agent kullanılır

📊 SONUÇ TİPLERİ
💰 Sponsorlu: Google reklamları (ads)
📌 Organik: Normal arama sonuçları
";
        tabHowTo.Controls.Add(txtHowTo);

        // ===== YASAL UYARI TAB =====
        var tabLegal = new TabPage
        {
            Text = "⚖️ Yasal Uyarı",
            BackColor = Color.White,
            Padding = new Padding(15)
        };
        tabControlInfo.TabPages.Add(tabLegal);

        var txtLegal = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(44, 62, 80),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        txtLegal.Text = @"⚖️ YASAL UYARI VE SORUMLULUK REDDİ
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📜 GENEL UYARI

Bu yazılım yalnızca eğitim ve araştırma amaçlı 
geliştirilmiştir. Kullanıcı, yazılımı kullanarak 
aşağıdaki şartları kabul etmiş sayılır.

⚠️ SORUMLULUK REDDİ

1. Yazılımın geliştiricileri, bu yazılımın kötü 
   niyetli veya yasadışı amaçlarla kullanılmasından 
   HİÇBİR ŞEKİLDE sorumlu tutulamaz.

2. Yazılımın kullanımından doğabilecek her türlü 
   yasal, mali veya teknik sorumluluk TAMAMEN 
   kullanıcıya aittir.

3. Bu yazılım 'OLDUĞU GİBİ' sunulmaktadır. Herhangi 
   bir garanti verilmemektedir.

4. Yazılımın Google veya herhangi bir üçüncü taraf 
   hizmetinin kullanım şartlarını ihlal edecek 
   şekilde kullanılması kullanıcının sorumluluğundadır.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🚫 YASAK KULLANIMLAR

• Rakiplere zarar vermek amacıyla kullanım
• Sahte trafik oluşturma
• Reklam sahtekarlığı
• Hizmet dışı bırakma saldırıları
• Herhangi bir yasadışı faaliyet

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ KABUL

Bu yazılımı kullanarak, yukarıdaki tüm şartları 
okuduğunuzu, anladığınızı ve kabul ettiğinizi 
beyan etmiş olursunuz.

© 2025 - Tüm hakları saklıdır.
";
        tabLegal.Controls.Add(txtLegal);

        // Kapat Butonu
        var btnClose = new Button
        {
            Text = "Anladım, Kapat",
            Location = new Point(220, 420),
            Size = new Size(150, 35),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = primaryColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => infoForm.Close();
        infoForm.Controls.Add(btnClose);

        infoForm.ShowDialog(this);
    }
}

public class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsSponsored { get; set; } = false;
}

public class ProxyInfo
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    
    public override string ToString() => $"{Host}:{Port}";
}
