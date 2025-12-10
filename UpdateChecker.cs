using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

namespace GoogleSearchApp;

/// <summary>
/// Uygulama güncelleme kontrolü ve yönetimi sınıfı
/// </summary>
public class UpdateChecker
{
    // ⚠️ ÖNEMLİ: Bu URL'yi kendi GitHub repository veya sunucunuzla değiştirin
    // Örnek: GitHub Raw URL veya kendi web sunucunuz
    private const string UPDATE_CHECK_URL = "https://raw.githubusercontent.com/Emre4321a/google-reklam/main/version.json";
    
    // Mevcut uygulama versiyonu - her güncelleme yaptığınızda artırın
    public static readonly Version CurrentVersion = new Version(2, 0, 0, 0);
    
    /// <summary>
    /// Güncelleme bilgisi modeli
    /// </summary>
    public class UpdateInfo
    {
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;
        
        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;
        
        [JsonProperty("mandatory")]
        public bool Mandatory { get; set; } = false;
        
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Güncelleme kontrolü yapar
    /// </summary>
    public static async Task<(bool hasUpdate, UpdateInfo? info)> CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "SponsorBotu-UpdateChecker");
            
            var response = await client.GetAsync(UPDATE_CHECK_URL);
            
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(jsonContent);
            
            if (updateInfo == null || string.IsNullOrEmpty(updateInfo.Version))
            {
                return (false, null);
            }
            
            var latestVersion = new Version(updateInfo.Version);
            bool hasUpdate = latestVersion > CurrentVersion;
            
            return (hasUpdate, hasUpdate ? updateInfo : null);
        }
        catch
        {
            // Güncelleme kontrolü başarısız olursa, uygulama normal çalışsın
            return (false, null);
        }
    }
    
    /// <summary>
    /// Güncelleme formunu gösterir
    /// </summary>
    public static DialogResult ShowUpdateDialog(UpdateInfo updateInfo, IWin32Window? owner = null)
    {
        var form = new Form
        {
            Text = "🔄 Güncelleme Mevcut!",
            Size = new Size(500, 380),
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
            BackColor = Color.FromArgb(46, 204, 113) // Yeşil
        };
        form.Controls.Add(headerPanel);
        
        var lblTitle = new Label
        {
            Text = "🎉 Yeni Güncelleme Mevcut!",
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
            Text = $"📦 Mevcut Versiyon: v{CurrentVersion}\n" +
                   $"🆕 Yeni Versiyon: v{updateInfo.Version}\n" +
                   $"📅 Yayın Tarihi: {updateInfo.ReleaseDate}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(44, 62, 80),
            Location = new Point(15, 15),
            Size = new Size(415, 70)
        };
        contentPanel.Controls.Add(lblVersionInfo);
        
        // Release Notes
        var lblNotesTitle = new Label
        {
            Text = "📋 Yenilikler:",
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
        
        DialogResult result = DialogResult.Cancel;
        
        // Buttons
        var btnUpdate = new Button
        {
            Text = "⬇️ Güncelle",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(130, 40),
            Location = new Point(130, 290),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnUpdate.FlatAppearance.BorderSize = 0;
        btnUpdate.Click += (s, e) => 
        {
            result = DialogResult.Yes;
            form.Close();
        };
        form.Controls.Add(btnUpdate);
        
        var btnLater = new Button
        {
            Text = "⏰ Sonra",
            Font = new Font("Segoe UI", 11),
            Size = new Size(100, 40),
            Location = new Point(270, 290),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = !updateInfo.Mandatory // Zorunlu güncelleme ise "Sonra" görünmez
        };
        btnLater.FlatAppearance.BorderSize = 0;
        btnLater.Click += (s, e) => 
        {
            result = DialogResult.No;
            form.Close();
        };
        form.Controls.Add(btnLater);
        
        // Zorunlu güncelleme uyarısı
        if (updateInfo.Mandatory)
        {
            var lblMandatory = new Label
            {
                Text = "⚠️ Bu güncelleme zorunludur. Devam etmek için güncellemeniz gerekiyor.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(231, 76, 60),
                Location = new Point(20, 335),
                Size = new Size(445, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            form.Controls.Add(lblMandatory);
            
            // Zorunlu güncellemede çarpı butonu kapatmayı engelle
            form.FormClosing += (s, e) =>
            {
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    MessageBox.Show("Bu güncelleme zorunludur. Devam etmek için güncellemeniz gerekiyor.",
                        "Zorunlu Güncelleme", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }
        
        form.ShowDialog(owner);
        return result;
    }
    
    /// <summary>
    /// Güncelleme indirme sayfasını açar
    /// </summary>
    public static void OpenDownloadPage(string downloadUrl)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = downloadUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"İndirme sayfası açılırken hata oluştu:\n{ex.Message}", 
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
                return true; // Güncelleme yok, devam et
            }
            
            var result = ShowUpdateDialog(updateInfo, owner);
            
            if (result == DialogResult.Yes)
            {
                OpenDownloadPage(updateInfo.DownloadUrl);
                return false; // Uygulama kapatılacak, güncelleme yapılacak
            }
            
            // Zorunlu güncelleme ise ve kullanıcı reddetmişse
            if (updateInfo.Mandatory && result != DialogResult.Yes)
            {
                return false; // Uygulama kapatılacak
            }
            
            return true; // Devam et
        }
        catch
        {
            // Güncelleme kontrolü başarısız olursa, uygulama normal çalışsın
            return true;
        }
    }
    
    /// <summary>
    /// Sessiz güncelleme kontrolü - sadece güncelleme varsa bildirir
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
        catch
        {
            // Sessiz modda hataları yoksay
        }
    }
}
