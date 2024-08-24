using System.Diagnostics;
using Newtonsoft.Json;

namespace FileDownloader
{
    public partial class Wc3CMCAUForm : Form
    {
        private Dictionary<string, string> downloadLinks;
        private CancellationTokenSource cts;
        private HttpClient httpClient;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Wc3CMCAUForm()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            InitializeComponent();
            httpClient = new HttpClient(); // Khởi tạo HttpClient
            cts = new CancellationTokenSource();
            InitializeCustomComponents();
            LoadDownloadLinks();
            VLKTComboBox.SelectedItem = "NON VIP";
            VLKTComboBox.Enabled = false;
        }

        private void InitializeCustomComponents()
        {
            // Configure Controls
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Step = 1;

            // Set default texts
            creditTextBox.Text = "- Wc3CMC Auto Update là phần mềm dùng để tải các Map Cheat Việt Nam do Wc3CMC thực hiện.\r\n- Map cheat sẽ được upload lên phần mềm và người dùng sẽ tải trực tiếp từ đây, không thông qua nhóm CMC nữa.\r\n- Ngoài ra, Wc3CMC Auto Update còn được tích hợp các phần mềm thông dụng khác dùng để Cheat.\r\n- Những hướng dẫn đã được ghi chú rõ ràng trong phần mềm và trong nhóm Wc3CMC's Playground.\n";
        }

        private async void LoadDownloadLinks()
        {
            var url = "https://raw.githubusercontent.com/kizl01/kizl01/main/version_download.json";
            try
            {
                var json = await httpClient.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new Exception("Dữ liệu tải xuống bị trống.");
                }

#pragma warning disable CS8601 // Possible null reference assignment.
                downloadLinks = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
#pragma warning restore CS8601 // Possible null reference assignment.

                if (downloadLinks == null || downloadLinks.Count == 0)
                {
                    throw new Exception("Không tìm thấy dữ liệu để tải xuống.");
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Không get được dữ liệu tải xuống do đường truyền mạng có vấn đề!");
            }
            catch (JsonException)
            {
                MessageBox.Show("Không kết nối được đến máy chủ lưu trữ!");
            }
            catch (Exception)
            {
                MessageBox.Show("Không thể tải được dữ liệu!");
            }
        }

        private async Task<string?> GetDecodedKey()
        {
            var keyUrl = "https://raw.githubusercontent.com/kizl01/kizl01/main/KEY.txt";
            try
            {
                var keyBase64 = await httpClient.GetStringAsync(keyUrl);
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(keyBase64.Trim()));
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Không thể tải được key");
                return null;
            }
            catch (FormatException)
            {
                MessageBox.Show("Định dạng key không hợp lệ");
                return null;
            }
        }

        private async Task HandleDownload(string key, string fileName, string url)
        {
            // Check if a download is in progress
            if (cts != null)
            {
                cts.Cancel(); // Cancel the previous download
                cts.Dispose();
            }
            cts = new CancellationTokenSource();
            var token = cts.Token;
            string basePath = Application.StartupPath;

            try
            {
                // Get the path to the destination file
                string destinationPath = Path.Combine(basePath, fileName);

                // Start downloading
                await DownloadFile(url, destinationPath, token);
            }
            catch (OperationCanceledException)
            {
                logTextBox.AppendText($"Đã dừng tải xuống {fileName}! {Environment.NewLine}");
                logTextBox2.AppendText($"Đã dừng tải xuống {fileName}! {Environment.NewLine}");
            }
            catch (Exception)
            {
                logTextBox.AppendText($"Tải xuống {fileName} thất bại! {Environment.NewLine}");
                logTextBox2.AppendText($"Tải xuống {fileName} thất bại! {Environment.NewLine}");
            }
        }

        private async void CheckForUpdatesButton_Click(object sender, EventArgs e)
        {
            if (downloadLinks == null)
            {
                MessageBox.Show("Không thể tải được dữ liệu!");
                return;
            }

            // Get the path to the directory where the executable is located
            string basePath = Application.StartupPath;

            // Prepare cancellation token
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                // Check if VLKT VIP is selected and require key input
                if (VLKT.Checked && VLKTComboBox.SelectedItem?.ToString() == "VIP")
                {
                    using (var keyInputForm = new KeyInputForm())
                    {
                        if (keyInputForm.ShowDialog() == DialogResult.OK)
                        {
                            string userKey = keyInputForm.UserKey;
                            string validKey = await GetDecodedKey();

                            if (userKey != validKey)
                            {
                                MessageBox.Show("Key không hợp lệ. Vui lòng kiểm tra lại.");
                                return;
                            }
                        }
                        else
                        {
                            // User canceled the key input
                            return;
                        }
                    }
                }

                // Proceed with file downloads
                if (CACK.Checked)
                    await TryDownloadFile("CACK", "CACK.zip", basePath, token);
                if (KVCT.Checked)
                    await TryDownloadFile("KVCT", "KVCT.zip", basePath, token);
                if (LTK.Checked)
                    await TryDownloadFile("LTK", "LTK.zip", basePath, token);
                if (VLKT.Checked)
                {
                    string key = VLKTComboBox.SelectedItem?.ToString() == "VIP" ? "VLKTVIP" : "VLKTNONVIP";
                    if (string.IsNullOrEmpty(key))
                    {
                        MessageBox.Show("Vui lòng chọn một phiên bản VLKT hợp lệ.");
                        return;
                    }
                    await TryDownloadFile(key, key + ".zip", basePath, token);
                }
            }
            catch (OperationCanceledException)
            {
                logTextBox.AppendText("Đã dừng tải xuống!" + Environment.NewLine);
                logTextBox2.AppendText("Đã dừng tải xuống!" + Environment.NewLine);
            }
            catch (Exception)
            {
                logTextBox.AppendText($"Tải xuống thất bại! {Environment.NewLine}");
                logTextBox2.AppendText($"Tải xuống thất bại! {Environment.NewLine}");
            }
        }


        private async Task TryDownloadFile(string prefix, string filename, string basePath, CancellationToken token)
        {
            var matchingKeys = downloadLinks.Keys.Where(k => k.StartsWith(prefix)).ToList();

            if (matchingKeys.Count > 0)
            {
                string matchingKey = matchingKeys.First();
                string url = downloadLinks[matchingKey];
                await DownloadFile(url, Path.Combine(basePath, filename), token);
            }
            else
            {
                logTextBox.AppendText($"Không tìm thấy phiên bản phù hợp. {Environment.NewLine}");
                logTextBox2.AppendText($"Không tìm thấy phiên bản phù hợp. {Environment.NewLine}");
            }
        }




        private async void Wc3CPIButton_Click(object sender, EventArgs e)
        {
            await HandleDownload("Wc3CPI", "Wc3CPI.zip", downloadLinks["Wc3CPI"]);
        }

        private async void Wc3CPIv2Button_Click(object sender, EventArgs e)
        {
            await HandleDownload("Wc3CPIv2", "Wc3CPIv2.zip", downloadLinks["Wc3CPIv2"]);
        }

        private async void MPQToolButton_Click(object sender, EventArgs e)
        {
            await HandleDownload("MPQTool", "MPQTool.zip", downloadLinks["MPQTool"]);
        }

        private async void JasscraftButton_Click(object sender, EventArgs e)
        {
            await HandleDownload("Jasscraft", "Jasscraft.zip", downloadLinks["Jasscraft"]);
        }

        private async void War3HelperButton_Click(object sender, EventArgs e)
        {
            await HandleDownload("War3Helper", "War3Helper.zip", downloadLinks["War3Helper"]);
        }

        private async void CMCTranslateButton_Click(object sender, EventArgs e)
        {
            await HandleDownload("CMCTranslate", "CMCTranslate.zip", downloadLinks["CMCTranslate"]);
        }

        private async Task DownloadFile(string url, string destinationPath, CancellationToken token)
        {
            try
            {
                using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long totalBytes = response.Content.Headers.ContentLength ?? -1;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                            totalBytesRead += bytesRead;

                            // Report progress
                            var progress = (int)(totalBytesRead * 100 / totalBytes);
                            progressBar.Value = progress;
                            progressBar2.Value = progress;
                            logTextBox.Invoke(new Action(() => logTextBox.Text = $"Đang tải xuống {Path.GetFileName(destinationPath)}: {progress}%{Environment.NewLine}"));
                            logTextBox2.Invoke(new Action(() => logTextBox2.Text = $"Đang tải xuống {Path.GetFileName(destinationPath)}: {progress}%{Environment.NewLine}"));

                            // Check for cancellation
                            if (token.IsCancellationRequested)
                            {
                                // Close the file stream and delete the partially downloaded file
                                fileStream.Close();
                                System.IO.File.Delete(destinationPath);
                                logTextBox.Invoke(new Action(() => logTextBox.AppendText($"Đã dừng tải xuống!{Environment.NewLine}")));
                                logTextBox2.Invoke(new Action(() => logTextBox2.AppendText($"Đã dừng tải xuống!{Environment.NewLine}")));
                                throw new OperationCanceledException(token);
                            }
                        }
                    }
                    logTextBox.Invoke(new Action(() => logTextBox.AppendText($"\nTải xuống hoàn tất!{Environment.NewLine}")));
                    logTextBox2.Invoke(new Action(() => logTextBox2.AppendText($"\nTải xuống hoàn tất!{Environment.NewLine}")));
                }
            }
            catch (OperationCanceledException)
            {
                // Exception is already handled, no need to re-handle here
            }
            catch (Exception)
            {
                logTextBox.Invoke(new Action(() =>
                {
                    logTextBox.AppendText($"Tải xuống thất bại! {Environment.NewLine}");
                    logTextBox2.AppendText($"Tải xuống thất bại! {Environment.NewLine}");
                }));
            }
        }

        private void VLKT_CheckedChanged(object sender, EventArgs e)
        {
            VLKTComboBox.Enabled = VLKT.Checked;
        }

        private void StopDownloadButton_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            logTextBox.AppendText($"Đã dừng tải xuống! {Environment.NewLine}");
            logTextBox2.AppendText($"Đã dừng tải xuống! {Environment.NewLine}");
        }

        private void StopDownload2Button_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            logTextBox.AppendText($"Đã dừng tải xuống! {Environment.NewLine}");
            logTextBox2.AppendText($"Đã dừng tải xuống! {Environment.NewLine}");
        }

        private void Wc3CMCAU_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://www.facebook.com/groups/wc3cmc";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở liên kết: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            httpClient?.Dispose();
            cts?.Dispose();
            base.OnFormClosing(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string url = "https://www.facebook.com/groups/wc3cmc";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở liên kết: " + ex.Message);
            }
        }
    }
}
