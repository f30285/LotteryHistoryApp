using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LotteryHistoryApp
{
    // 配置管理类
    public class AppConfig
    {
        public string Token { get; set; } = "";
        public DateTime TokenUpdateTime { get; set; } = DateTime.Now;

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LotteryApp", "config.json");

        // 保存配置
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 加载配置
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return new AppConfig();
        }
    }

    // Token设置对话框
    public partial class TokenSettingsForm : Form
    {
        private TextBox txtToken;
        private Button btnOK, btnCancel, btnTest;
        private Label lblInfo, lblCurrentToken;
        private readonly HttpClient httpClient;

        public string Token { get; private set; }

        public TokenSettingsForm(string currentToken, HttpClient client)
        {
            httpClient = client;
            Token = currentToken;
            InitializeComponent();
            LoadCurrentToken();
        }

        private void InitializeComponent()
        {
            this.Text = "Token设置";
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 当前Token显示
            lblCurrentToken = new Label()
            {
                Text = "当前Token:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(460, 20),
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold)
            };

            // Token输入框
            lblInfo = new Label()
            {
                Text = "请输入新的Token:",
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(200, 20)
            };

            txtToken = new TextBox()
            {
                Location = new System.Drawing.Point(20, 105),
                Size = new System.Drawing.Size(440, 23),
                Font = new System.Drawing.Font("Consolas", 9F)
            };

            // 按钮
            btnTest = new Button()
            {
                Text = "测试Token",
                Location = new System.Drawing.Point(20, 150),
                Size = new System.Drawing.Size(100, 30)
            };
            btnTest.Click += BtnTest_Click;

            btnOK = new Button()
            {
                Text = "保存",
                Location = new System.Drawing.Point(280, 200),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button()
            {
                Text = "取消",
                Location = new System.Drawing.Point(380, 200),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] {
                lblCurrentToken, lblInfo, txtToken, btnTest, btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadCurrentToken()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                lblCurrentToken.Text = $"当前Token: {Token.Substring(0, Math.Min(50, Token.Length))}...";
                txtToken.Text = Token;
            }
            else
            {
                lblCurrentToken.Text = "当前Token: 未设置";
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            string testToken = txtToken.Text.Trim();
            if (string.IsNullOrEmpty(testToken))
            {
                MessageBox.Show("请先输入Token!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "测试中...";

            try
            {
                // 测试Token是否有效
                bool isValid = await TestTokenAsync(testToken);
                if (isValid)
                {
                    MessageBox.Show("Token测试成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Token测试失败，请检查Token是否正确！", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "测试Token";
            }
        }

        private async Task<bool> TestTokenAsync(string token)
        {
            try
            {
                // 创建临时HttpClient进行测试
                using (var testClient = new HttpClient())
                {
                    // 复制所有headers
                    testClient.DefaultRequestHeaders.Clear();
                    testClient.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
                    testClient.DefaultRequestHeaders.Add("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
                    testClient.DefaultRequestHeaders.Add("device", "1");
                    testClient.DefaultRequestHeaders.Add("lang", "zh_cn");
                    testClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Google Chrome\";v=\"138\"");
                    testClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
                    testClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
                    testClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
                    testClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
                    testClient.DefaultRequestHeaders.Add("sec-fetch-site", "cross-site");
                    testClient.DefaultRequestHeaders.Add("timezone", "GMT+8");
                    testClient.DefaultRequestHeaders.Add("token", token); // 使用测试Token
                    testClient.DefaultRequestHeaders.Add("Referer", "https://b02pc.jqrrub.com/");

                    // 测试一个API请求
                    var response = await testClient.GetAsync("https://b02api-im.h7ief2.com/coron/trendGraph/chart/history?ticketId=168&num=5");
                    var content = await response.Content.ReadAsStringAsync();

                    // 检查是否包含token失效信息
                    if (content.Contains("token invalid") || content.Contains("token expire"))
                        return false;

                    // 尝试解析JSON
                    var json = JObject.Parse(content);
                    return json["code"]?.Value<int>() != -1;
                }
            }
            catch
            {
                return false;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string newToken = txtToken.Text.Trim();
            if (string.IsNullOrEmpty(newToken))
            {
                MessageBox.Show("Token不能为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Token = newToken;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // 增强版概率分析类
    public class EnhancedProbabilityAnalyzer
    {
        // 加权概率计算（核心优化）
        public Dictionary<string, double> WeightedBigSmallProbability(List<string> data)
        {
            if (data == null || data.Count == 0) return CreateDefaultProb();

            double bigWeightedSum = 0;
            double totalWeight = 0;

            // 指数衰减权重：越近期权重越高
            for (int i = 0; i < data.Count; i++)
            {
                if (int.TryParse(data[i], out int num))
                {
                    // 权重公式：0.95的幂，最新数据权重接近1，越老权重越小
                    double weight = Math.Pow(0.95, data.Count - 1 - i);
                    totalWeight += weight;
                    if (num >= 5) // 大数
                        bigWeightedSum += weight;
                }
            }

            if (totalWeight == 0) return CreateDefaultProb();

            double bigProb = bigWeightedSum / totalWeight;

            // 应用趋势修正
            bigProb = ApplyTrendCorrection(data, bigProb);

            // 确保概率在合理范围内
            bigProb = Math.Max(0.15, Math.Min(0.85, bigProb));

            return new Dictionary<string, double>
            {
                { "大", bigProb },
                { "小", 1 - bigProb }
            };
        }

        // 加权单双概率计算
        public Dictionary<string, double> WeightedOddEvenProbability(List<string> data)
        {
            if (data == null || data.Count == 0) return CreateDefaultProb();

            double oddWeightedSum = 0;
            double totalWeight = 0;

            for (int i = 0; i < data.Count; i++)
            {
                if (int.TryParse(data[i], out int num))
                {
                    double weight = Math.Pow(0.95, data.Count - 1 - i);
                    totalWeight += weight;
                    if (num % 2 == 1) // 单数
                        oddWeightedSum += weight;
                }
            }

            if (totalWeight == 0) return CreateDefaultProb();

            double oddProb = oddWeightedSum / totalWeight;

            // 应用趋势修正
            oddProb = ApplyTrendCorrection(data, oddProb, true);
            oddProb = Math.Max(0.15, Math.Min(0.85, oddProb));

            return new Dictionary<string, double>
            {
                { "单", oddProb },
                { "双", 1 - oddProb }
            };
        }

        // 多周期综合分析
        public Dictionary<string, double> MultiPeriodBigSmallProbability(List<string> data)
        {
            if (data == null || data.Count < 10) return CreateDefaultProb();

            // 获取不同周期的数据
            var shortTerm = data.Skip(Math.Max(0, data.Count - 10)).ToList();
            var mediumTerm = data.Skip(Math.Max(0, data.Count - 30)).ToList();
            var longTerm = data.Skip(Math.Max(0, data.Count - 50)).ToList();

            // 计算各周期概率
            var shortProb = WeightedBigSmallProbability(shortTerm)["大"];
            var mediumProb = WeightedBigSmallProbability(mediumTerm)["大"];
            var longProb = WeightedBigSmallProbability(longTerm)["大"];

            // 加权融合：短期权重最高
            double finalBigProb = (shortProb * 0.5) + (mediumProb * 0.3) + (longProb * 0.2);

            // 应用连续性修正
            finalBigProb = ApplyContinuityCorrection(data, finalBigProb);
            finalBigProb = Math.Max(0.15, Math.Min(0.85, finalBigProb));

            return new Dictionary<string, double>
            {
                { "大", finalBigProb },
                { "小", 1 - finalBigProb }
            };
        }

        // 趋势修正：检测并调整过度倾向
        private double ApplyTrendCorrection(List<string> data, double baseProb, bool isOddEven = false)
        {
            if (data.Count < 5) return baseProb;

            var recent5 = data.Skip(Math.Max(0, data.Count - 5)).ToList();
            int consecutiveCount = 0;
            bool targetCondition = false;

            // 检测连续性
            for (int i = recent5.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(recent5[i], out int num))
                {
                    bool currentCondition = isOddEven ? (num % 2 == 1) : (num >= 5);

                    if (i == recent5.Count - 1)
                    {
                        targetCondition = currentCondition;
                        consecutiveCount = 1;
                    }
                    else if (currentCondition == targetCondition)
                    {
                        consecutiveCount++;
                    }
                    else break;
                }
            }

            // 连续3次以上，适当降低继续的概率
            if (consecutiveCount >= 3)
            {
                double correction = -0.08 * (consecutiveCount - 2); // 递减修正
                if (targetCondition) // 如果最近都是大/单，降低大/单的概率
                    return Math.Max(0.15, baseProb + correction);
                else // 如果最近都是小/双，降低小/双的概率
                    return Math.Min(0.85, baseProb - correction);
            }

            return baseProb;
        }

        // 连续性修正：避免预测过度极端
        private double ApplyContinuityCorrection(List<string> data, double prob)
        {
            if (data.Count < 3) return prob;

            var recent3 = data.Skip(Math.Max(0, data.Count - 3)).Select(x => int.Parse(x)).ToList();
            bool allBig = recent3.All(x => x >= 5);
            bool allSmall = recent3.All(x => x < 5);

            if (allBig && prob > 0.6) // 连续都是大，且预测概率还很高
                return Math.Max(0.4, prob - 0.15);
            if (allSmall && prob < 0.4) // 连续都是小，且预测概率还很低
                return Math.Min(0.6, prob + 0.15);

            return prob;
        }

        // 默认概率（50:50）
        private Dictionary<string, double> CreateDefaultProb()
        {
            return new Dictionary<string, double> { { "大", 0.5 }, { "小", 0.5 } };
        }

        // 保持向后兼容的原方法
        public Dictionary<string, double> BigSmallProbability(List<string> data)
        {
            return MultiPeriodBigSmallProbability(data);
        }

        public Dictionary<string, double> OddEvenProbability(List<string> data)
        {
            return WeightedOddEvenProbability(data);
        }

        // 其他辅助方法
        public Dictionary<int, int> CountFrequencies(List<string> data)
        {
            var freq = new Dictionary<int, int>();
            foreach (var s in data)
            {
                if (int.TryParse(s, out int d) && d >= 0 && d <= 9)
                {
                    if (!freq.ContainsKey(d)) freq[d] = 0;
                    freq[d]++;
                }
            }
            return freq;
        }

        public Dictionary<int, double> ComputeDigitProbabilities(List<string> data)
        {
            var freq = CountFrequencies(data);
            int total = freq.Values.Sum();
            var prob = new Dictionary<int, double>();

            for (int i = 0; i <= 9; i++)
            {
                prob[i] = freq.ContainsKey(i) ? (double)freq[i] / total : 0d;
            }

            return prob;
        }

        public double RecentTrend<T>(List<T> data, Func<T, bool> attrFunc)
        {
            if (data == null || data.Count < 10) return -1;

            var recent10 = data.Skip(Math.Max(0, data.Count - 10))
                .Where(x => int.TryParse(x.ToString(), out _))
                .Select(x => int.Parse(x.ToString()))
                .ToList();

            if (recent10.Count < 10) return -1;

            int count = recent10.Count(x => attrFunc((T)(object)x));
            return count / 10.0;
        }
    }

    public partial class MainForm : Form
    {
        private const string urlHNCQ = "https://b02api-im.h7ief2.com/coron/trendGraph/chart/history?ticketId=168&num=50";
        private const string urlXYYF = "https://b02api-im.h7ief2.com/coron/trendGraph/chart/history?ticketId=45&num=50";
        private const string urlJSSS = "https://b02api-im.h7ief2.com/coron/trendGraph/chart/history?ticketId=2&num=50";
        private const string urlTXFF = "https://b02api-im.h7ief2.com/coron/trendGraph/chart/history?ticketId=57&num=50";

        private readonly HttpClient httpClient;
        private TableLayoutPanel tableLayoutPanel;
        private Panel panelHNCQ, panelXYYF, panelJSSS, panelTXFF, panelSummary, panelStats, panelVerification;
        private Label lblHNCQ, lblXYYF, lblJSSS, lblTXFF, lblSummary;
        private DataGridView dgvHNCQ, dgvXYYF, dgvJSSS, dgvTXFF, dgvSummary;
        private TabControl tabControlStats;
        private Dictionary<string, DataGridView> statsGrids;

        // 预测验证相关
        private DataGridView dgvVerification, dgvConsecutive;
        private List<PredictionVerificationRecord> verificationRecords;

        // 连中连挂统计
        private Dictionary<string, ConsecutiveRecord> consecutiveRecords = new Dictionary<string, ConsecutiveRecord>();

        // 保存的预测结果
        private List<SavedPrediction> savedPredictions = new List<SavedPrediction>();

        // 配置管理
        private AppConfig appConfig;
        private MenuStrip menuStrip;

        // 自动刷新相关字段
        private System.Windows.Forms.Timer autoRefreshTimer;
        private bool isAutoRefreshEnabled = true; // 默认开启自动刷新
        private bool isRefreshing = false; // 防止重复刷新的标志

        // 状态栏相关
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel autoRefreshStatusLabel;

        // 保存的预测结果类
        public class SavedPrediction
        {
            public string LotteryType { get; set; }
            public string PredictedPeriod { get; set; } // 预测的期号
            public string PredictedItem { get; set; }
            public double Probability { get; set; }
            public DateTime PredictTime { get; set; }
            public bool IsVerified { get; set; } = false;
        }

        // 预测结果项
        public class PredictionItem
        {
            public string Description { get; set; } // 描述，如"第1位-大"
            public double Probability { get; set; } // 概率
            public string Position { get; set; } // 位置
            public string Type { get; set; } // 类型：大小或单双
            public string Value { get; set; } // 值：大/小/单/双
        }

        // 预测验证记录
        public class PredictionVerificationRecord
        {
            public string Period { get; set; } // 期号
            public string LotteryType { get; set; } // 彩种
            public string PredictedItem { get; set; } // 预测项
            public double Probability { get; set; } // 预测概率
            public string ActualResult { get; set; } // 实际结果
            public bool IsCorrect { get; set; } // 是否命中
            public DateTime RecordTime { get; set; } // 记录时间
        }

        // 连中连挂记录
        public class ConsecutiveRecord
        {
            public int CurrentConsecutiveWins { get; set; } // 当前连中次数
            public int CurrentConsecutiveLosses { get; set; } // 当前连挂次数
            public int MaxConsecutiveWins { get; set; } // 最大连中次数
            public int MaxConsecutiveLosses { get; set; } // 最大连挂次数
            public int TotalPredictions { get; set; } // 总预测次数
            public int TotalWins { get; set; } // 总命中次数
            public double WinRate => TotalPredictions > 0 ? (double)TotalWins / TotalPredictions : 0;
        }

        // Top1预测汇总数据结构
        public class Top1PredictionSummary
        {
            public string LotteryType { get; set; }
            public string PredictedPeriod { get; set; }
            public string PredictedItem { get; set; }
            public double Probability { get; set; }
            public string Recommendation { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        // 存储Top1预测汇总
        private List<Top1PredictionSummary> top1Summaries = new List<Top1PredictionSummary>();

        public MainForm()
        {
            // 加载配置
            appConfig = AppConfig.Load();

            InitializeComponent();
            InitializeMenu();
            InitializeAutoRefresh(); // 初始化自动刷新
            InitializeStatusStrip(); // 初始化状态栏

            httpClient = new HttpClient();

            // 检查Token
            if (!CheckAndSetupToken())
            {
                this.WindowState = FormWindowState.Minimized;
                return;
            }

            SetupHttpHeaders();
            SetDataGridViewCellStyleCenter();
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 初始化验证记录
            verificationRecords = new List<PredictionVerificationRecord>();
            InitializeSummaryPanel();
            InitializeStatsPanel();
            InitializeVerificationPanel();

            this.Resize += MainForm_Resize;

            this.Load += async (s, e) => await RefreshAllDataAsync();
        }
        // 使用 Resize 事件处理窗体状态变化
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (autoRefreshTimer != null)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    // 最小化时暂停自动刷新（节省资源）
                    if (isAutoRefreshEnabled)
                    {
                        autoRefreshTimer.Stop();
                    }
                }
                else if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
                {
                    // 恢复时重启自动刷新
                    if (isAutoRefreshEnabled)
                    {
                        autoRefreshTimer.Start();
                    }
                }
            }
        }
        // 初始化自动刷新定时器
        private void InitializeAutoRefresh()
        {
            autoRefreshTimer = new System.Windows.Forms.Timer();
            autoRefreshTimer.Interval = 5000; // 5秒
            autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            // 启动自动刷新（如果开启）
            if (isAutoRefreshEnabled)
            {
                autoRefreshTimer.Start();
            }
        }

        // 自动刷新定时器事件
        private async void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            // 如果正在刷新中，跳过本次刷新
            if (isRefreshing)
                return;

            try
            {
                await RefreshAllDataAsync();
            }
            catch (Exception ex)
            {
                // 自动刷新出错时，静默处理，避免频繁弹窗
                UpdateStatusDisplay($"自动刷新失败: {ex.Message}");
                Console.WriteLine($"自动刷新失败: {ex.Message}");
            }
        }

        // 初始化状态栏
        private void InitializeStatusStrip()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "就绪";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            autoRefreshStatusLabel = new ToolStripStatusLabel();
            autoRefreshStatusLabel.Text = isAutoRefreshEnabled ? "自动刷新: 开启" : "自动刷新: 关闭";
            autoRefreshStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;

            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, autoRefreshStatusLabel });

            this.Controls.Add(statusStrip);
        }

        // 更新状态栏显示
        private void UpdateStatusDisplay(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
            }

            if (autoRefreshStatusLabel != null)
            {
                autoRefreshStatusLabel.Text = isAutoRefreshEnabled ? "自动刷新: 开启" : "自动刷新: 关闭";
            }
        }

        private void InitializeComponent()
        {
            this.tableLayoutPanel = new TableLayoutPanel();
            this.panelHNCQ = new Panel();
            this.panelXYYF = new Panel();
            this.panelJSSS = new Panel();
            this.panelTXFF = new Panel();
            this.panelSummary = new Panel();
            this.panelStats = new Panel();
            this.panelVerification = new Panel();
            this.lblHNCQ = new Label();
            this.lblXYYF = new Label();
            this.lblJSSS = new Label();
            this.lblTXFF = new Label();
            this.lblSummary = new Label();
            this.dgvHNCQ = new DataGridView();
            this.dgvXYYF = new DataGridView();
            this.dgvJSSS = new DataGridView();
            this.dgvTXFF = new DataGridView();
            this.dgvSummary = new DataGridView();

            // TableLayoutPanel设置 - 修改为5行布局
            this.tableLayoutPanel.Dock = DockStyle.Fill;
            this.tableLayoutPanel.RowCount = 5;
            this.tableLayoutPanel.ColumnCount = 2;

            // 修改行高比例：历史数据增大，汇总面板缩小
            this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 18F)); // 第一行：20%（增大）
            this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 18F)); // 第二行：20%（增大）
            this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 18F)); // 汇总：15%（缩小）
            this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15F)); // 预测统计：30%（保持）
            this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // 验证连挂：15%（缩小）

            this.tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Panel[] panels = { panelHNCQ, panelXYYF, panelJSSS, panelTXFF, panelSummary, panelStats, panelVerification };
            foreach (var panel in panels)
            {
                panel.Dock = DockStyle.Fill;
                panel.BorderStyle = BorderStyle.FixedSingle;
            }

            panelSummary.BackColor = System.Drawing.Color.LightCoral; // 汇总面板红色背景
            panelStats.BackColor = System.Drawing.Color.LightGreen;
            panelVerification.BackColor = System.Drawing.Color.LightBlue;

            lblHNCQ.Text = "河内1分彩";
            lblXYYF.Text = "幸运分分彩";
            lblJSSS.Text = "极速时时彩";
            lblTXFF.Text = "腾讯分分彩";
            lblSummary.Text = "【最佳预测汇总】";

            Label[] labels = { lblHNCQ, lblXYYF, lblJSSS, lblTXFF, lblSummary };
            foreach (var label in labels)
            {
                label.Dock = DockStyle.Top;
                label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                label.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
                label.Height = 24;
            }

            // 汇总标签特殊样式
            lblSummary.BackColor = System.Drawing.Color.DarkRed;
            lblSummary.ForeColor = System.Drawing.Color.White;

            DataGridView[] dgvArray = { dgvHNCQ, dgvXYYF, dgvJSSS, dgvTXFF };
            foreach (var dgv in dgvArray)
            {
                dgv.Dock = DockStyle.Fill;
                dgv.ReadOnly = true;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToDeleteRows = false;
                dgv.RowHeadersVisible = false;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.AutoGenerateColumns = true;
            }

            panelHNCQ.Controls.Add(dgvHNCQ);
            panelHNCQ.Controls.Add(lblHNCQ);
            panelXYYF.Controls.Add(dgvXYYF);
            panelXYYF.Controls.Add(lblXYYF);
            panelJSSS.Controls.Add(dgvJSSS);
            panelJSSS.Controls.Add(lblJSSS);
            panelTXFF.Controls.Add(dgvTXFF);
            panelTXFF.Controls.Add(lblTXFF);

            // 添加汇总面板
            panelSummary.Controls.Add(dgvSummary);
            panelSummary.Controls.Add(lblSummary);

            tableLayoutPanel.Controls.Add(panelHNCQ, 0, 0);
            tableLayoutPanel.Controls.Add(panelXYYF, 1, 0);
            tableLayoutPanel.Controls.Add(panelJSSS, 0, 1);
            tableLayoutPanel.Controls.Add(panelTXFF, 1, 1);
            tableLayoutPanel.Controls.Add(panelSummary, 0, 2);
            tableLayoutPanel.SetColumnSpan(panelSummary, 2);
            tableLayoutPanel.Controls.Add(panelStats, 0, 3);
            tableLayoutPanel.SetColumnSpan(panelStats, 2);
            tableLayoutPanel.Controls.Add(panelVerification, 0, 4);
            tableLayoutPanel.SetColumnSpan(panelVerification, 2);

            this.Controls.Add(tableLayoutPanel);
            this.Text = "彩票历史数据及概率统计展示";
            this.MinimumSize = new System.Drawing.Size(900, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        // 初始化菜单（添加自动刷新控制）
        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();

            // 设置菜单
            var settingsMenu = new ToolStripMenuItem("设置(&S)");

            var tokenMenuItem = new ToolStripMenuItem("Token管理(&T)");
            tokenMenuItem.Click += TokenMenuItem_Click;
            tokenMenuItem.ShortcutKeys = Keys.Control | Keys.T;

            // 自动刷新控制菜单项
            var autoRefreshMenuItem = new ToolStripMenuItem("自动刷新(&A)");
            autoRefreshMenuItem.Checked = isAutoRefreshEnabled;
            autoRefreshMenuItem.Click += AutoRefreshMenuItem_Click;
            autoRefreshMenuItem.ShortcutKeys = Keys.Control | Keys.A;

            var refreshMenuItem = new ToolStripMenuItem("立即刷新(&R)");
            refreshMenuItem.Click += async (s, e) => await RefreshAllDataAsync();
            refreshMenuItem.ShortcutKeys = Keys.F5;

            settingsMenu.DropDownItems.AddRange(new ToolStripItem[] {
                tokenMenuItem,
                new ToolStripSeparator(),
                autoRefreshMenuItem,
                refreshMenuItem
            });

            // 帮助菜单
            var helpMenu = new ToolStripMenuItem("帮助(&H)");
            var aboutMenuItem = new ToolStripMenuItem("关于(&A)");
            aboutMenuItem.Click += (s, e) => MessageBox.Show(
                "彩票历史数据及概率统计展示系统\n版本: 1.0\n\n" +
                "功能说明:\n" +
                "• 自动刷新：每5秒自动获取最新数据\n" +
                "• 手动刷新：F5键或菜单刷新\n\n" +
                "快捷键:\n" +
                "F5 - 立即刷新数据\n" +
                "Ctrl+T - Token管理\n" +
                "Ctrl+A - 开启/关闭自动刷新",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new ToolStripItem[] { settingsMenu, helpMenu });

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        // 自动刷新菜单点击事件
        private void AutoRefreshMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null)
            {
                // 切换自动刷新状态
                isAutoRefreshEnabled = !isAutoRefreshEnabled;
                menuItem.Checked = isAutoRefreshEnabled;

                if (isAutoRefreshEnabled)
                {
                    autoRefreshTimer.Start();
                    this.Text = "彩票历史数据及概率统计展示 [自动刷新:开启]";
                }
                else
                {
                    autoRefreshTimer.Stop();
                    this.Text = "彩票历史数据及概率统计展示 [自动刷新:关闭]";
                }

                UpdateStatusDisplay(isAutoRefreshEnabled ? "自动刷新已开启" : "自动刷新已关闭");
            }
        }

        // Token菜单点击事件
        private async void TokenMenuItem_Click(object sender, EventArgs e)
        {
            await ShowTokenSettings();
        }

        // 显示Token设置对话框
        private async Task ShowTokenSettings()
        {
            using (var form = new TokenSettingsForm(appConfig.Token, httpClient))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // 更新Token
                    appConfig.Token = form.Token;
                    appConfig.TokenUpdateTime = DateTime.Now;
                    appConfig.Save();

                    // 更新HTTP头
                    SetupHttpHeaders();

                    MessageBox.Show("Token已更新！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatusDisplay("Token已更新");

                    // 自动刷新数据
                    await RefreshAllDataAsync();
                }
            }
        }

        // 检查并设置Token
        private bool CheckAndSetupToken()
        {
            // 如果没有Token或Token为空，显示设置对话框
            while (string.IsNullOrEmpty(appConfig.Token))
            {
                var result = MessageBox.Show(
                    "系统需要Token才能正常工作，是否现在设置？",
                    "Token设置",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    return false; // 用户不想设置，退出
                }

                // 显示Token设置对话框
                using (var form = new TokenSettingsForm(appConfig.Token, new HttpClient()))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        appConfig.Token = form.Token;
                        appConfig.TokenUpdateTime = DateTime.Now;
                        appConfig.Save();
                        break;
                    }
                    else
                    {
                        // 用户取消了设置
                        return false;
                    }
                }
            }

            return true;
        }

        // 设置HTTP头（使用配置中的Token）
        private void SetupHttpHeaders()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
            httpClient.DefaultRequestHeaders.Add("device", "1");
            httpClient.DefaultRequestHeaders.Add("lang", "zh_cn");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Google Chrome\";v=\"138\"");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "cross-site");
            httpClient.DefaultRequestHeaders.Add("timezone", "GMT+8");
            httpClient.DefaultRequestHeaders.Add("token", appConfig.Token); // 使用配置中的Token
            httpClient.DefaultRequestHeaders.Add("Referer", "https://b02pc.jqrrub.com/");
        }

        private void SetDataGridViewCellStyleCenter()
        {
            var style = new DataGridViewCellStyle()
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            dgvHNCQ.DefaultCellStyle = style;
            dgvXYYF.DefaultCellStyle = style;
            dgvJSSS.DefaultCellStyle = style;
            dgvTXFF.DefaultCellStyle = style;
            dgvSummary.DefaultCellStyle = style;

            dgvHNCQ.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvXYYF.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvJSSS.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvTXFF.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSummary.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        // 初始化汇总面板
        private void InitializeSummaryPanel()
        {
            dgvSummary.Dock = DockStyle.Fill;
            dgvSummary.ReadOnly = true;
            dgvSummary.AllowUserToAddRows = false;
            dgvSummary.AllowUserToDeleteRows = false;
            dgvSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvSummary.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSummary.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSummary.RowHeadersVisible = false;
            dgvSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 添加汇总表格列
            dgvSummary.Columns.Add("彩种", "彩种");
            dgvSummary.Columns.Add("预测期数", "预测期数");
            dgvSummary.Columns.Add("预测项", "预测项");
            dgvSummary.Columns.Add("概率", "概率");
            dgvSummary.Columns.Add("推荐度", "推荐度");
            dgvSummary.Columns.Add("更新时间", "更新时间");

            // 设置列宽
            dgvSummary.Columns[0].Width = 120; // 彩种
            dgvSummary.Columns[1].Width = 120; // 预测期数
            dgvSummary.Columns[2].Width = 100; // 预测项
            dgvSummary.Columns[3].Width = 80;  // 概率
            dgvSummary.Columns[4].Width = 150; // 推荐度
            dgvSummary.Columns[5].Width = 120; // 更新时间
            dgvSummary.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // 设置汇总表格特殊样式
            dgvSummary.GridColor = System.Drawing.Color.DarkRed;
            dgvSummary.BackgroundColor = System.Drawing.Color.MistyRose;
            dgvSummary.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.IndianRed;
            dgvSummary.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            dgvSummary.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
        }

        private void InitializeStatsPanel()
        {
            tabControlStats = new TabControl()
            {
                Dock = DockStyle.Fill
            };

            statsGrids = new Dictionary<string, DataGridView>();
            string[] lotNames = { "河内1分彩", "幸运分分彩", "极速时时彩", "腾讯分分彩" };

            // 创建各彩种预测Tab页
            foreach (string name in lotNames)
            {
                TabPage tab = new TabPage(name);
                DataGridView dgv = new DataGridView()
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                    ColumnHeadersDefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };

                // 添加列 - 新增"预测期数"列并放在最前面
                dgv.Columns.Add("预测期数", "预测期数");  // 新增的列，放在第一位
                dgv.Columns.Add("排名", "排名");
                dgv.Columns.Add("预测项", "预测项");
                dgv.Columns.Add("概率", "概率");
                dgv.Columns.Add("推荐度", "推荐度");

                // 通过索引设置列宽
                dgv.Columns[0].Width = 120; // 预测期数列
                dgv.Columns[1].Width = 60;  // 排名列
                dgv.Columns[2].Width = 100; // 预测项列
                dgv.Columns[3].Width = 80;  // 概率列
                dgv.Columns[4].Width = 200; // 推荐度列
                dgv.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // 推荐度列填充剩余空间

                tab.Controls.Add(dgv);
                tabControlStats.TabPages.Add(tab);
                statsGrids.Add(name, dgv);
            }

            panelStats.Controls.Add(tabControlStats);
        }

        // 初始化验证和连挂面板
        private void InitializeVerificationPanel()
        {
            // 创建左右分割的TabControl
            TabControl tabVerification = new TabControl()
            {
                Dock = DockStyle.Fill
            };

            // 预测验证Tab页
            TabPage tabPageVerification = new TabPage("预测验证");
            dgvVerification = new DataGridView()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersDefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // 添加验证结果列
            dgvVerification.Columns.Add("期号", "期号");
            dgvVerification.Columns.Add("彩种", "彩种");
            dgvVerification.Columns.Add("预测项", "预测项");
            dgvVerification.Columns.Add("概率", "概率");
            dgvVerification.Columns.Add("实际结果", "实际结果");
            dgvVerification.Columns.Add("命中", "命中");
            dgvVerification.Columns.Add("时间", "时间");

            // 设置列宽
            dgvVerification.Columns[0].Width = 120; // 期号
            dgvVerification.Columns[1].Width = 100; // 彩种
            dgvVerification.Columns[2].Width = 100; // 预测项
            dgvVerification.Columns[3].Width = 80;  // 概率
            dgvVerification.Columns[4].Width = 100; // 实际结果
            dgvVerification.Columns[5].Width = 60;  // 命中
            dgvVerification.Columns[6].Width = 140; // 时间
            dgvVerification.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            tabPageVerification.Controls.Add(dgvVerification);

            // 连中连挂Tab页
            TabPage tabPageConsecutive = new TabPage("连中连挂");
            dgvConsecutive = new DataGridView()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // 添加连中连挂统计列
            dgvConsecutive.Columns.Add("彩种", "彩种");
            dgvConsecutive.Columns.Add("总预测", "总预测");
            dgvConsecutive.Columns.Add("总命中", "总命中");
            dgvConsecutive.Columns.Add("命中率", "命中率");
            dgvConsecutive.Columns.Add("当前连中", "当前连中");
            dgvConsecutive.Columns.Add("当前连挂", "当前连挂");
            dgvConsecutive.Columns.Add("最大连中", "最大连中");
            dgvConsecutive.Columns.Add("最大连挂", "最大连挂");

            tabPageConsecutive.Controls.Add(dgvConsecutive);

            // 添加到TabControl
            tabVerification.TabPages.Add(tabPageVerification);
            tabVerification.TabPages.Add(tabPageConsecutive);

            panelVerification.Controls.Add(tabVerification);
        }

        private async void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                await RefreshAllDataAsync();
                e.Handled = true;
            }
        }

        // 刷新数据方法（添加刷新状态控制）
        private async Task RefreshAllDataAsync()
        {
            // 防止重复刷新
            if (isRefreshing)
                return;

            isRefreshing = true;
            this.Enabled = false;

            // 更新标题显示刷新状态
            string originalTitle = this.Text;
            this.Text = originalTitle.Replace("[自动刷新:开启]", "[刷新中...]").Replace("[自动刷新:关闭]", "[刷新中...]");
            if (!this.Text.Contains("[刷新中...]"))
            {
                this.Text += " [刷新中...]";
            }

            UpdateStatusDisplay("正在刷新数据...");

            try
            {
                var t1 = LoadLotteryDataAsync(urlHNCQ);
                var t2 = LoadLotteryDataAsync(urlXYYF);
                var t3 = LoadLotteryDataAsync(urlJSSS);
                var t4 = LoadLotteryDataAsync(urlTXFF);

                await Task.WhenAll(t1, t2, t3, t4);

                dgvHNCQ.DataSource = null;
                dgvHNCQ.Columns.Clear();
                dgvHNCQ.AutoGenerateColumns = true;
                dgvHNCQ.DataSource = t1.Result;

                dgvXYYF.DataSource = null;
                dgvXYYF.Columns.Clear();
                dgvXYYF.AutoGenerateColumns = true;
                dgvXYYF.DataSource = t2.Result;

                dgvJSSS.DataSource = null;
                dgvJSSS.Columns.Clear();
                dgvJSSS.AutoGenerateColumns = true;
                dgvJSSS.DataSource = t3.Result;

                dgvTXFF.DataSource = null;
                dgvTXFF.Columns.Clear();
                dgvTXFF.AutoGenerateColumns = true;
                dgvTXFF.DataSource = t4.Result;

                AutoSizeAllColumns();

                // 核心逻辑：先验证，再预测
                PerformPredictionAndVerification();
                UpdateSummaryDisplay();
                UpdateVerificationDisplay();
                UpdateConsecutiveDisplay();

                UpdateStatusDisplay("数据刷新完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据刷新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatusDisplay($"刷新失败: {ex.Message}");
            }
            finally
            {
                isRefreshing = false;
                this.Enabled = true;

                // 恢复标题
                if (isAutoRefreshEnabled)
                {
                    this.Text = "彩票历史数据及概率统计展示 [自动刷新:开启]";
                }
                else
                {
                    this.Text = "彩票历史数据及概率统计展示 [自动刷新:关闭]";
                }
            }
        }

        private void AutoSizeAllColumns()
        {
            dgvHNCQ.AutoResizeColumns();
            dgvXYYF.AutoResizeColumns();
            dgvJSSS.AutoResizeColumns();
            dgvTXFF.AutoResizeColumns();
        }

        // 数据加载方法（添加Token失效检测）
        private async Task<DataTable> LoadLotteryDataAsync(string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                string jsonStr = await response.Content.ReadAsStringAsync();

                // 检测Token失效
                if (jsonStr.Contains("token invalid") || jsonStr.Contains("token expire"))
                {
                    // Token失效，提示用户更新
                    var result = MessageBox.Show(
                        "Token已失效，是否现在更新Token？",
                        "Token失效",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        await ShowTokenSettings();

                        // 重新尝试请求
                        response = await httpClient.GetAsync(url);
                        jsonStr = await response.Content.ReadAsStringAsync();

                        // 再次检查
                        if (jsonStr.Contains("token invalid") || jsonStr.Contains("token expire"))
                        {
                            throw new Exception("Token仍然无效，请检查Token是否正确！");
                        }
                    }
                    else
                    {
                        throw new Exception("Token已失效，无法获取数据！");
                    }
                }

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"请求失败，状态码：{response.StatusCode}，返回内容：{jsonStr}");

                JObject json = JObject.Parse(jsonStr);
                if (json["code"]?.Value<int>() == -1)
                {
                    string msg = json["msg"]?.Value<string>() ?? "未知错误";
                    throw new Exception($"API返回错误：{msg}");
                }

                var dataArr = json["data"] as JArray;
                if (dataArr == null)
                    throw new Exception("返回数据格式异常，未包含data字段");

                DataTable dt = new DataTable();
                dt.Columns.Add("期号", typeof(string));
                dt.Columns.Add("第一位", typeof(string));
                dt.Columns.Add("第二位", typeof(string));
                dt.Columns.Add("第三位", typeof(string));
                dt.Columns.Add("第四位", typeof(string));
                dt.Columns.Add("第五位", typeof(string));

                foreach (var item in dataArr)
                {
                    string issue = item["issue"]?.ToString() ?? "";
                    string code = item["code"]?.ToString() ?? "";
                    var nums = code.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    string[] pos = new string[5];
                    for (int i = 0; i < 5; i++)
                        pos[i] = i < nums.Length ? nums[i] : "";

                    dt.Rows.Add(issue, pos[0], pos[1], pos[2], pos[3], pos[4]);
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据加载失败：{ex.Message}");
            }
        }

        // 执行预测和验证
        private void PerformPredictionAndVerification()
        {
            var nameToDGV = new Dictionary<string, DataGridView>
            {
                { "河内1分彩", dgvHNCQ },
                { "幸运分分彩", dgvXYYF },
                { "极速时时彩", dgvJSSS },
                { "腾讯分分彩", dgvTXFF }
            };

            var analyzer = new EnhancedProbabilityAnalyzer();
            top1Summaries.Clear(); // 清空之前的汇总

            foreach (var kv in nameToDGV)
            {
                string lotName = kv.Key;
                var dgvNumbers = kv.Value;
                var dgvStats = statsGrids[lotName];

                dgvStats.Rows.Clear();

                if (dgvNumbers.ColumnCount < 6 || dgvNumbers.Rows.Count < 2)
                    continue;

                // 获取最新一期开奖结果
                var latestRow = dgvNumbers.Rows[0];
                string latestPeriod = latestRow.Cells[0].Value?.ToString() ?? "";

                // 第一步：验证之前的预测（如果存在）
                VerifyPreviousPredictions(lotName, latestPeriod, latestRow);

                // 第二步：基于所有数据进行新的预测
                var allPredictions = new List<PredictionItem>();
                for (int pos = 1; pos <= 5; pos++)
                {
                    List<string> allColData = new List<string>();
                    foreach (DataGridViewRow row in dgvNumbers.Rows)
                    {
                        if (row.Cells[pos].Value != null)
                            allColData.Add(row.Cells[pos].Value.ToString());
                    }

                    if (allColData.Count < 10) continue; // 数据不足，跳过

                    // 计算大小概率
                    var bigSmall = analyzer.MultiPeriodBigSmallProbability(allColData);
                    double bigProb = bigSmall["大"];
                    double smallProb = bigSmall["小"];

                    // 计算单双概率
                    var oddEven = analyzer.WeightedOddEvenProbability(allColData);
                    double oddProb = oddEven["单"];
                    double evenProb = oddEven["双"];

                    // 添加预测项
                    allPredictions.Add(new PredictionItem
                    {
                        Description = $"第{pos}位-大",
                        Probability = bigProb,
                        Position = $"第{pos}位",
                        Type = "大小",
                        Value = "大"
                    });

                    allPredictions.Add(new PredictionItem
                    {
                        Description = $"第{pos}位-小",
                        Probability = smallProb,
                        Position = $"第{pos}位",
                        Type = "大小",
                        Value = "小"
                    });

                    allPredictions.Add(new PredictionItem
                    {
                        Description = $"第{pos}位-单",
                        Probability = oddProb,
                        Position = $"第{pos}位",
                        Type = "单双",
                        Value = "单"
                    });

                    allPredictions.Add(new PredictionItem
                    {
                        Description = $"第{pos}位-双",
                        Probability = evenProb,
                        Position = $"第{pos}位",
                        Type = "单双",
                        Value = "双"
                    });
                }

                // 显示Top5预测结果
                var displayTop5 = allPredictions
                    .OrderByDescending(p => p.Probability)
                    .Take(5)
                    .ToList();

                // 计算下一期期号并保存预测
                string nextPeriod = CalculateNextPeriod(latestPeriod);

                if (displayTop5.Count > 0)
                {
                    // 保存第一名预测用于下次验证
                    var topPrediction = displayTop5[0];

                    // 检查是否已经有这个期号的预测，避免重复保存
                    bool alreadyPredicted = savedPredictions.Any(p =>
                        p.LotteryType == lotName &&
                        p.PredictedPeriod == nextPeriod &&
                        p.PredictedItem == topPrediction.Description);

                    if (!alreadyPredicted)
                    {
                        savedPredictions.Add(new SavedPrediction
                        {
                            LotteryType = lotName,
                            PredictedPeriod = nextPeriod,
                            PredictedItem = topPrediction.Description,
                            Probability = topPrediction.Probability,
                            PredictTime = DateTime.Now
                        });
                    }

                    // 保存Top1预测到汇总列表
                    var existingSummary = top1Summaries.FirstOrDefault(s => s.LotteryType == lotName);
                    if (existingSummary != null)
                    {
                        // 更新现有记录
                        existingSummary.PredictedPeriod = nextPeriod;
                        existingSummary.PredictedItem = topPrediction.Description;
                        existingSummary.Probability = topPrediction.Probability;
                        existingSummary.Recommendation = GetRecommendationLevel(topPrediction.Probability);
                        existingSummary.UpdateTime = DateTime.Now;
                    }
                    else
                    {
                        // 添加新记录
                        top1Summaries.Add(new Top1PredictionSummary
                        {
                            LotteryType = lotName,
                            PredictedPeriod = nextPeriod,
                            PredictedItem = topPrediction.Description,
                            Probability = topPrediction.Probability,
                            Recommendation = GetRecommendationLevel(topPrediction.Probability),
                            UpdateTime = DateTime.Now
                        });
                    }
                }

                // 显示预测结果
                for (int i = 0; i < displayTop5.Count; i++)
                {
                    var prediction = displayTop5[i];
                    string recommendation = GetRecommendationLevel(prediction.Probability);
                    string displayText = prediction.Description;
                    if (i == 0)
                    {
                        displayText += " (预测下期)";
                    }

                    dgvStats.Rows.Add(
                        nextPeriod,              // 预测期数（新增列）
                        $"#{i + 1}",            // 排名
                        displayText,            // 预测项
                        prediction.Probability.ToString("P1"), // 概率
                        recommendation          // 推荐度
                    );
                }
            }

            // 清理过期的预测记录（超过7天）
            savedPredictions.RemoveAll(p => DateTime.Now - p.PredictTime > TimeSpan.FromDays(7));

            // 只保留最近40条验证记录
            if (verificationRecords.Count > 40)
            {
                verificationRecords = verificationRecords
                    .OrderByDescending(r => r.RecordTime)
                    .Take(40)
                    .ToList();
            }
        }

        // 更新汇总显示
        private void UpdateSummaryDisplay()
        {
            dgvSummary.Rows.Clear();

            // 按彩种顺序显示
            string[] lotOrder = { "河内1分彩", "幸运分分彩", "极速时时彩", "腾讯分分彩" };

            foreach (string lotName in lotOrder)
            {
                var summary = top1Summaries.FirstOrDefault(s => s.LotteryType == lotName);
                if (summary != null)
                {
                    dgvSummary.Rows.Add(
                        summary.LotteryType,
                        summary.PredictedPeriod,
                        summary.PredictedItem,
                        summary.Probability.ToString("P1"),
                        summary.Recommendation,
                        summary.UpdateTime.ToString("HH:mm:ss")
                    );

                    // 根据推荐度设置行颜色
                    var row = dgvSummary.Rows[dgvSummary.Rows.Count - 1];
                    if (summary.Probability >= 0.75)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen; // 强推
                        row.DefaultCellStyle.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
                    }
                    else if (summary.Probability >= 0.65)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow; // 推荐
                    }
                    else if (summary.Probability < 0.45)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray; // 不推
                    }
                }
                else
                {
                    // 如果没有预测数据，显示空行
                    dgvSummary.Rows.Add(lotName, "暂无数据", "-", "-", "-", "-");
                    var row = dgvSummary.Rows[dgvSummary.Rows.Count - 1];
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                }
            }
        }

        // 验证之前的预测方法
        private void VerifyPreviousPredictions(string lotName, string currentPeriod, DataGridViewRow currentRow)
        {
            // 查找对应当前期号的预测
            var predictionsToVerify = savedPredictions
                .Where(p => p.LotteryType == lotName &&
                           p.PredictedPeriod == currentPeriod &&
                           !p.IsVerified)
                .ToList();

            foreach (var prediction in predictionsToVerify)
            {
                // 解析预测项
                var parts = prediction.PredictedItem.Split('-');
                if (parts.Length == 2)
                {
                    string position = parts[0]; // "第1位"
                    string predictedValue = parts[1]; // "大" 或 "小" 或 "单" 或 "双"

                    // 获取对应位置的实际号码
                    int posIndex = GetPositionIndex(position);
                    if (posIndex >= 1 && posIndex <= 5)
                    {
                        string actualNumber = currentRow.Cells[posIndex].Value?.ToString() ?? "";
                        if (int.TryParse(actualNumber, out int actualNum))
                        {
                            // 判断实际结果
                            string actualResult = GetActualResult(actualNum, predictedValue);
                            bool isCorrect = predictedValue == actualResult;

                            // 记录验证结果
                            var record = new PredictionVerificationRecord
                            {
                                Period = currentPeriod,
                                LotteryType = lotName,
                                PredictedItem = prediction.PredictedItem,
                                Probability = prediction.Probability,
                                ActualResult = $"{position}-{actualResult}",
                                IsCorrect = isCorrect,
                                RecordTime = DateTime.Now
                            };

                            verificationRecords.Add(record);

                            // 更新连中连挂统计
                            UpdateConsecutiveRecord(lotName, isCorrect);

                            // 标记为已验证
                            prediction.IsVerified = true;
                        }
                    }
                }
            }
        }

        // 期号计算方法
        private string CalculateNextPeriod(string currentPeriod)
        {
            if (string.IsNullOrEmpty(currentPeriod))
                return "未知";

            try
            {
                // 解析带有破折号的期号格式 (如 "20250724-0964")
                if (currentPeriod.Contains("-"))
                {
                    string[] parts = currentPeriod.Split('-');
                    if (parts.Length == 2)
                    {
                        string datePart = parts[0]; // 日期部分 (20250724)
                        string seqPart = parts[1];  // 序号部分 (0964)

                        if (int.TryParse(seqPart, out int seqNum))
                        {
                            const int maxSeqNum = 1440; // 一天最大期号，根据实际彩种调整
                            int seqLength = seqPart.Length;

                            // 日期转换为DateTime
                            if (DateTime.TryParseExact(datePart, "yyyyMMdd",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out DateTime date))
                            {
                                if (seqNum >= maxSeqNum)
                                {
                                    // 到达当天最后一期，跳到下一天第一期
                                    DateTime nextDay = date.AddDays(1);
                                    string nextDatePart = nextDay.ToString("yyyyMMdd");
                                    string nextSeqPart = 1.ToString().PadLeft(seqLength, '0'); // 重置为 "0001"
                                    return $"{nextDatePart}-{nextSeqPart}";
                                }
                                else
                                {
                                    // 未到最后一期，序号+1，并保持前导零
                                    string nextSeqPart = (seqNum + 1).ToString().PadLeft(seqLength, '0');
                                    return $"{datePart}-{nextSeqPart}";
                                }
                            }
                        }
                    }
                }

                // 如果格式不匹配，尝试直接增加数字
                if (long.TryParse(currentPeriod, out long periodNum))
                {
                    return (periodNum + 1).ToString();
                }
            }
            catch
            {
                // 解析失败，返回原期号 + "+1"
            }

            return currentPeriod + "+1";
        }

        // 更新连中连挂记录
        private void UpdateConsecutiveRecord(string lotName, bool isCorrect)
        {
            if (!consecutiveRecords.ContainsKey(lotName))
            {
                consecutiveRecords[lotName] = new ConsecutiveRecord();
            }

            var record = consecutiveRecords[lotName];
            record.TotalPredictions++;

            if (isCorrect)
            {
                record.TotalWins++;
                record.CurrentConsecutiveWins++;
                record.CurrentConsecutiveLosses = 0;

                if (record.CurrentConsecutiveWins > record.MaxConsecutiveWins)
                {
                    record.MaxConsecutiveWins = record.CurrentConsecutiveWins;
                }
            }
            else
            {
                record.CurrentConsecutiveLosses++;
                record.CurrentConsecutiveWins = 0;

                if (record.CurrentConsecutiveLosses > record.MaxConsecutiveLosses)
                {
                    record.MaxConsecutiveLosses = record.CurrentConsecutiveLosses;
                }
            }
        }

        // 更新连中连挂显示
        private void UpdateConsecutiveDisplay()
        {
            if (dgvConsecutive == null) return;

            dgvConsecutive.Rows.Clear();

            string[] lotNames = { "河内1分彩", "幸运分分彩", "极速时时彩", "腾讯分分彩" };

            foreach (string lotName in lotNames)
            {
                var record = consecutiveRecords.ContainsKey(lotName) ? consecutiveRecords[lotName] : new ConsecutiveRecord();

                dgvConsecutive.Rows.Add(
                    lotName,
                    record.TotalPredictions.ToString(),
                    record.TotalWins.ToString(),
                    record.WinRate.ToString("P1"),
                    record.CurrentConsecutiveWins.ToString(),
                    record.CurrentConsecutiveLosses.ToString(),
                    record.MaxConsecutiveWins.ToString(),
                    record.MaxConsecutiveLosses.ToString()
                );

                // 根据当前状态设置行颜色
                var row = dgvConsecutive.Rows[dgvConsecutive.Rows.Count - 1];
                if (record.CurrentConsecutiveWins > 0)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen; // 连中
                }
                else if (record.CurrentConsecutiveLosses > 0)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightPink; // 连挂
                }
            }
        }

        // 获取位置索引
        private int GetPositionIndex(string position)
        {
            if (position.Contains("第1位")) return 1;
            if (position.Contains("第2位")) return 2;
            if (position.Contains("第3位")) return 3;
            if (position.Contains("第4位")) return 4;
            if (position.Contains("第5位")) return 5;
            return 0;
        }

        // 获取实际结果
        private string GetActualResult(int number, string predictedType)
        {
            if (predictedType == "大" || predictedType == "小")
            {
                return number >= 5 ? "大" : "小";
            }
            else if (predictedType == "单" || predictedType == "双")
            {
                return number % 2 == 1 ? "单" : "双";
            }

            return "";
        }

        // 更新验证结果显示
        private void UpdateVerificationDisplay()
        {
            if (dgvVerification == null) return;

            dgvVerification.Rows.Clear();

            // 显示最近的验证记录（按时间倒序）
            var recentRecords = verificationRecords
                .OrderByDescending(r => r.RecordTime)
                .Take(20) // 最多显示20条记录
                .ToList();

            foreach (var record in recentRecords)
            {
                dgvVerification.Rows.Add(
                    record.Period,
                    record.LotteryType,
                    record.PredictedItem,
                    record.Probability.ToString("P1"),
                    record.ActualResult,
                    record.IsCorrect ? "✓" : "✗",
                    record.RecordTime.ToString("MM-dd HH:mm")
                );

                // 设置命中行的背景色
                var row = dgvVerification.Rows[dgvVerification.Rows.Count - 1];
                if (record.IsCorrect)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightPink;
                }
            }

            // 更新Tab页标题显示准确率
            UpdateVerificationTabTitle();
        }

        // 更新验证Tab页标题，显示准确率
        private void UpdateVerificationTabTitle()
        {
            if (verificationRecords.Count == 0) return;

            int totalRecords = verificationRecords.Count;
            int correctRecords = verificationRecords.Count(r => r.IsCorrect);
            double accuracy = (double)correctRecords / totalRecords;

            // 查找并更新Tab页标题
            foreach (Control control in panelVerification.Controls)
            {
                if (control is TabControl tabControl)
                {
                    foreach (TabPage tab in tabControl.TabPages)
                    {
                        if (tab.Text.StartsWith("预测验证"))
                        {
                            tab.Text = $"预测验证 ({correctRecords}/{totalRecords} - {accuracy:P1})";
                            break;
                        }
                    }
                }
            }
        }

        // 根据概率获取推荐等级
        private string GetRecommendationLevel(double probability)
        {
            if (probability >= 0.75) return "★★★★★ 强推";
            if (probability >= 0.65) return "★★★★☆ 推荐";
            if (probability >= 0.55) return "★★★☆☆ 一般";
            if (probability >= 0.45) return "★★☆☆☆ 观望";
            return "★☆☆☆☆ 不推";
        }

        // 窗体关闭时停止定时器
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (autoRefreshTimer != null)
            {
                autoRefreshTimer.Stop();
                autoRefreshTimer.Dispose();
            }

            if (httpClient != null)
            {
                httpClient.Dispose();
            }

            base.OnFormClosed(e);
        }
    }
}
