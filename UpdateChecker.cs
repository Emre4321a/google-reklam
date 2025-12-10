using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleSearchApp;

/// <summary>
/// Uygulama güncelleme kontrolü ve yönetimi sınıfı
/// </summary>
public class UpdateChecker
{
    // GitHub API URL - Repo'nun PUBLIC olması gerekiyor!
    private const string GITHUB_API_URL = "https://api.github.com/repos/Emre4321a/google-reklam/releases/latest";
    
    // Mevcut uygulama versiyonu - her güncelleme yaptığınızda artırın
    public static readonly Version CurrentVersion = new Version(2, 0, 0, 2);
    
    /// <summary>
    /// Güncelleme bilgisi modeli
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool Mandatory { get; set; } = false;
        public string ReleaseDate { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// GitHub Releases API'den güncelleme kontrolü yapar
    /// </summary>
    public static async Task<(bool hasUpdate, UpdateInfo? info)> CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "SponsorBotu-UpdateChecker");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            
            var response = await client.GetAsync(GITHUB_API_URL);
            
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            var release = JObject.Parse(jsonContent);
            
            // Tag name'den versiyon al (v2.0.0.1 -> 2.0.0.1)
            var tagName = release["tag_name"]?.ToString() ?? "";
            var versionString = tagName.TrimStart('v', 'V');
            
            if (string.IsNullOrEmpty(versionString))
            {
                return (false, null);
            }
            
            // Assets'ten zip dosyasını bul
            var assets = release["assets"] as JArray;
            string downloadUrl = "";
            string assetName = "";
            
            if (assets != null && assets.Count > 0)
            {
                foreach (var asset in assets)
                {
                    var name = asset["name"]?.ToString() ?? "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset["browser_download_url"]?.ToString() ?? "";
                        assetName = name;
                        break;
                    }
                }
            }
            
            // Zip bulunamadıysa zipball_url kullan
            if (string.IsNullOrEmpty(downloadUrl))
            {
                downloadUrl = release["zipball_url"]?.ToString() ?? "";
                assetName = $"sponsor-botu-{versionString}.zip";
            }
            
            var updateInfo = new UpdateInfo
            {
                Version = versionString,
                DownloadUrl = downloadUrl,
                ReleaseNotes = release["body"]?.ToString() ?? "Yeni güncelleme mevcut.",
                ReleaseDate = release["published_at"]?.ToString()?.Split('T')[0] ?? DateTime.Now.ToString("yyyy-MM-dd"),
                AssetName = assetName,
                Mandatory = false
            };
            
            var latestVersion = new Version(versionString);
            bool hasUpdate = latestVersion > CurrentVersion;
            
            return (hasUpdate, hasUpdate ? updateInfo : null);
        }
        catch
        {
            return (false, null);
        }
    }
    
    /// <summary>
    /// Güncellemeyi indirir ve kurar
    /// </summary>
    public static async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "SponsorBotuUpdate");
        string zipPath = Path.Combine(tempPath, "update.zip");
        string extractPath = Path.Combine(tempPath, "extracted");
        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        string updaterBatPath = Path.Combine(tempPath, "updater.bat");
        
        try
        {
            // Temp klasörünü temizle ve oluştur
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(extractPath);
            
            progress?.Report(10);
            
            // Dosyayı indir
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "SponsorBotu-UpdateChecker");
            
            using var response = await client.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var downloadedBytes = 0L;
            
            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[8192];
                int bytesRead;
                
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var percentage = (int)(10 + (downloadedBytes * 50 / totalBytes));
                        progress?.Report(Math.Min(percentage, 60));
                    }
                }
            }
            
            progress?.Report(65);
            
            // Zip'i çıkart
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            progress?.Report(80);
            
            // Çıkartılan klasörü bul (GitHub zipball'da klasör içinde gelir)
            var extractedDirs = Directory.GetDirectories(extractPath);
            string sourceDir = extractedDirs.Length > 0 ? extractedDirs[0] : extractPath;
            
            // publish veya bin klasörünü ara
            var publishDir = Directory.GetDirectories(sourceDir, "publish", SearchOption.AllDirectories).FirstOrDefault();
            var binDir = Directory.GetDirectories(sourceDir, "bin", SearchOption.AllDirectories).FirstOrDefault();
            
            if (!string.IsNullOrEmpty(publishDir))
                sourceDir = publishDir;
            else if (!string.IsNullOrEmpty(binDir))
            {
                var releaseDir = Directory.GetDirectories(binDir, "Release", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(releaseDir))
                {
                    var net481Dir = Directory.GetDirectories(releaseDir, "net481", SearchOption.AllDirectories).FirstOrDefault();
                    sourceDir = net481Dir ?? releaseDir;
                }
            }
            
            progress?.Report(85);
            
            // Exe dosyasının bulunduğu klasör
            string appExePath = Path.Combine(appPath.TrimEnd('\\'), "GoogleSearchApp.exe");
            
            // Updater batch dosyası oluştur - short path kullanarak Türkçe karakter sorununu önle
            var batchContent = $@"@echo off
chcp 1254 >nul
echo Guncelleme kuruluyor, lutfen bekleyin...
ping 127.0.0.1 -n 3 >nul

:waitloop
tasklist /FI ""IMAGENAME eq GoogleSearchApp.exe"" 2>NUL | find /I /N ""GoogleSearchApp.exe"">NUL
if ""%ERRORLEVEL%""==""0"" (
    echo Uygulama kapatiliyor...
    taskkill /F /IM GoogleSearchApp.exe >nul 2>&1
    ping 127.0.0.1 -n 2 >nul
    goto waitloop
)

echo Dosyalar kopyalaniyor...
xcopy /E /Y /Q ""{sourceDir}\*"" ""{appPath.TrimEnd('\\')}\"" >nul 2>&1
if errorlevel 1 (
    copy /Y ""{sourceDir}\*.*"" ""{appPath.TrimEnd('\\')}\""  >nul 2>&1
)

echo Guncelleme tamamlandi!
ping 127.0.0.1 -n 2 >nul

echo Uygulama yeniden baslatiliyor...
cd /d ""{appPath.TrimEnd('\\')}""
start """" ""GoogleSearchApp.exe""

ping 127.0.0.1 -n 3 >nul
rd /s /q ""{tempPath}"" 2>nul
exit
";
            
            File.WriteAllText(updaterBatPath, batchContent, System.Text.Encoding.GetEncoding(1254));
            progress?.Report(95);
            
            // Batch dosyasını çalıştır
            var psi = new ProcessStartInfo
            {
                FileName = updaterBatPath,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            
            Process.Start(psi);
            progress?.Report(100);
            
            // Uygulamayı kapat
            Application.Exit();
            
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Güncelleme sırasında hata oluştu:\n{ex.Message}", 
                "Güncelleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            // Temizlik
            try
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
            catch { }
            
            return false;
        }
    }
    
    /// <summary>
    /// Güncelleme formunu gösterir
    /// </summary>
    public static DialogResult ShowUpdateDialog(UpdateInfo updateInfo, IWin32Window? owner = null)
    {
        var form = new Form
        {
            Text = "Güncelleme Mevcut!",
            Size = new Size(500, 420),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(236, 240, 241),
            Icon = SystemIcons.Information
        };
        
        // Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(46, 204, 113)
        };
        form.Controls.Add(headerPanel);
        
        var lblTitle = new Label
        {
            Text = "Yeni Güncelleme Mevcut!",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };
        headerPanel.Controls.Add(lblTitle);
        
        // Content Panel
        var contentPanel = new Panel
        {
            Location = new Point(20, 85),
            Size = new Size(445, 190),
            BackColor = Color.White
        };
        form.Controls.Add(contentPanel);
        
        // Version Info
        var lblVersionInfo = new Label
        {
            Text = $"Mevcut Versiyon: v{CurrentVersion}\n" +
                   $"Yeni Versiyon: v{updateInfo.Version}\n" +
                   $"Yayın Tarihi: {updateInfo.ReleaseDate}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(44, 62, 80),
            Location = new Point(15, 15),
            Size = new Size(415, 70)
        };
        contentPanel.Controls.Add(lblVersionInfo);
        
        // Release Notes
        var lblNotesTitle = new Label
        {
            Text = "Yenilikler:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            Location = new Point(15, 90),
            AutoSize = true
        };
        contentPanel.Controls.Add(lblNotesTitle);
        
        var txtNotes = new TextBox
        {
            Text = updateInfo.ReleaseNotes,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(15, 115),
            Size = new Size(415, 60),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle
        };
        contentPanel.Controls.Add(txtNotes);
        
        // Progress Bar
        var progressBar = new ProgressBar
        {
            Location = new Point(20, 290),
            Size = new Size(445, 25),
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };
        form.Controls.Add(progressBar);
        
        var lblProgress = new Label
        {
            Text = "İndiriliyor...",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(44, 62, 80),
            Location = new Point(20, 318),
            AutoSize = true,
            Visible = false
        };
        form.Controls.Add(lblProgress);
        
        DialogResult result = DialogResult.Cancel;
        
        // Buttons
        var btnUpdate = new Button
        {
            Text = "Güncelle",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(130, 40),
            Location = new Point(130, 340),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnUpdate.FlatAppearance.BorderSize = 0;
        form.Controls.Add(btnUpdate);
        
        var btnLater = new Button
        {
            Text = "Sonra",
            Font = new Font("Segoe UI", 11),
            Size = new Size(100, 40),
            Location = new Point(270, 340),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = !updateInfo.Mandatory
        };
        btnLater.FlatAppearance.BorderSize = 0;
        form.Controls.Add(btnLater);
        
        btnUpdate.Click += async (s, e) =>
        {
            btnUpdate.Enabled = false;
            btnLater.Enabled = false;
            progressBar.Visible = true;
            lblProgress.Visible = true;
            
            var progressReporter = new Progress<int>(percent =>
            {
                if (progressBar.InvokeRequired)
                {
                    progressBar.Invoke(new Action(() => progressBar.Value = percent));
                    lblProgress.Invoke(new Action(() => 
                    {
                        if (percent < 60) lblProgress.Text = $"İndiriliyor... %{percent}";
                        else if (percent < 85) lblProgress.Text = "Dosyalar çıkartılıyor...";
                        else if (percent < 95) lblProgress.Text = "Güncelleme hazırlanıyor...";
                        else lblProgress.Text = "Uygulama yeniden başlatılıyor...";
                    }));
                }
                else
                {
                    progressBar.Value = percent;
                    if (percent < 60) lblProgress.Text = $"İndiriliyor... %{percent}";
                    else if (percent < 85) lblProgress.Text = "Dosyalar çıkartılıyor...";
                    else if (percent < 95) lblProgress.Text = "Güncelleme hazırlanıyor...";
                    else lblProgress.Text = "Uygulama yeniden başlatılıyor...";
                }
            });
            
            var success = await DownloadAndInstallUpdateAsync(updateInfo, progressReporter);
            
            if (!success)
            {
                btnUpdate.Enabled = true;
                btnLater.Enabled = true;
                progressBar.Visible = false;
                lblProgress.Visible = false;
            }
        };
        
        btnLater.Click += (s, e) =>
        {
            result = DialogResult.No;
            form.Close();
        };
        
        if (updateInfo.Mandatory)
        {
            form.FormClosing += (s, e) =>
            {
                if (result != DialogResult.Yes && !btnUpdate.Enabled)
                {
                    return; // Güncelleme devam ediyorsa kapatmaya izin ver
                }
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    MessageBox.Show("Bu güncelleme zorunludur.", "Zorunlu Güncelleme", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }
        
        form.ShowDialog(owner);
        return result;
    }
    
    /// <summary>
    /// Uygulama başlangıcında güncelleme kontrolü yapar
    /// </summary>
    public static async Task<bool> CheckAndPromptForUpdateAsync(IWin32Window? owner = null)
    {
        try
        {
            var (hasUpdate, updateInfo) = await CheckForUpdateAsync();
            
            if (!hasUpdate || updateInfo == null)
            {
                return true;
            }
            
            ShowUpdateDialog(updateInfo, owner);
            return true;
        }
        catch
        {
            return true;
        }
    }
    
    /// <summary>
    /// Sessiz güncelleme kontrolü
    /// </summary>
    public static async Task CheckForUpdateSilentlyAsync(Action<UpdateInfo>? onUpdateAvailable = null)
    {
        try
        {
            var (hasUpdate, updateInfo) = await CheckForUpdateAsync();
            
            if (hasUpdate && updateInfo != null)
            {
                onUpdateAvailable?.Invoke(updateInfo);
            }
        }
        catch { }
    }
}
