namespace GoogleSearchApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Splash/Loading formu göster
        using var splashForm = new SplashForm();
        splashForm.Show();
        Application.DoEvents();
        
        // Güncelleme kontrolü yap (ConfigureAwait(false) ile deadlock önlenir)
        splashForm.UpdateStatus("Güncelleme kontrol ediliyor...");
        
        try
        {
            var updateTask = Task.Run(async () => await UpdateChecker.CheckForUpdateAsync());
            var (hasUpdate, updateInfo) = updateTask.GetAwaiter().GetResult();
            
            if (hasUpdate && updateInfo != null)
            {
                splashForm.Hide();
                // ShowUpdateDialog içinde otomatik indirme ve kurulum yapılıyor
                UpdateChecker.ShowUpdateDialog(updateInfo, null);
                // Dialog açıldıysa ve güncelleme başladıysa uygulama zaten kapanacak
            }
        }
        catch
        {
            // Güncelleme kontrolü başarısız olursa devam et
        }
        
        splashForm.Show();
        splashForm.UpdateStatus("Uygulama başlatılıyor...");
        Application.DoEvents();
        System.Threading.Thread.Sleep(300);
        
        splashForm.Close();
        
        // Ana formu çalıştır
        Application.Run(new MainForm());
    }
}

/// <summary>
/// Uygulama açılışında gösterilen splash/loading formu
/// </summary>
public class SplashForm : Form
{
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
    
    public SplashForm()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        this.Text = "";
        this.Size = new Size(400, 200);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(44, 62, 80);
        this.ShowInTaskbar = false;
        
        // Logo/Başlık
        var lblTitle = new Label
        {
            Text = "Sponsor Botu",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 55,
            Padding = new Padding(0, 15, 0, 0)
        };
        this.Controls.Add(lblTitle);
        
        // Alt başlık
        var lblSubtitle = new Label
        {
            Text = "SEO Traffic Tool",
            Font = new Font("Segoe UI", 10, FontStyle.Italic),
            ForeColor = Color.FromArgb(149, 165, 166),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 65),
            Size = new Size(400, 25)
        };
        this.Controls.Add(lblSubtitle);
        
        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(50, 110),
            Size = new Size(300, 8),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };
        this.Controls.Add(progressBar);
        
        // Durum Label
        lblStatus = new Label
        {
            Text = "Başlatılıyor...",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(189, 195, 199),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 130),
            Size = new Size(400, 25)
        };
        this.Controls.Add(lblStatus);
        
        // Versiyon bilgisi
        var lblVersion = new Label
        {
            Text = $"v{UpdateChecker.CurrentVersion}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(127, 140, 141),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 165),
            Size = new Size(400, 20)
        };
        this.Controls.Add(lblVersion);
    }
    
    public void UpdateStatus(string status)
    {
        if (lblStatus.InvokeRequired)
        {
            lblStatus.Invoke(() => UpdateStatus(status));
            return;
        }
        
        lblStatus.Text = status;
        Application.DoEvents();
    }
}
