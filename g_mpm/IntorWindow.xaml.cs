using g_mpm.Enums;
using g_mpm.Structs;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public partial class IntorWindow : Window
    {
        // 共享内存相关字段
        private SharedMemoryLauncher _launcher;
        private DispatcherTimer _statusTimer;

        // 世界显示相关字段
        private List<WorldCardItem> worldCards = new List<WorldCardItem>();
        private double worldNextY = 50;
        private WorldCardItem? _selectedWorldCard = null;

        // 玩家显示相关字段
        private List<PlayerCardItem> playerCards = new List<PlayerCardItem>();
        private double playerNextY = 50;
        private PlayerCardItem? _selectedPlayerCard = null;

        // 滚动相关字段
        private ScrollViewer _playerScrollViewer;
        private ScrollViewer _worldScrollViewer;
        private Canvas _playerCanvas;
        private Canvas _worldCanvas;

        public bool flish = false;

        public enum ButtonStatic : int
        {
            About = 1,
            Level2
        };

        public ButtonStatic bs;

        // 用于等待命令完成的TaskCompletionSource
        private TaskCompletionSource<bool>? _listWorldCompletionSource;
        private TaskCompletionSource<bool>? _listPlayerCompletionSource;

        public IntorWindow()
        {
            InitializeComponent();

            // 初始化共享内存组件
            InitializeLauncher();
            SetupStatusTimer();

            // 启动时自动完成初始化
            this.Loaded += async (s, e) => await InitializeOnStartup();
            this.Closing += Window_Closing;

            // 初始化PlayerGrid和WorldGrid
            InitializePlayerGrid();
            InitializeWorldGrid();

            Storyboard sb = (Storyboard)this.Resources["Intor"];
            sb.SpeedRatio = 0.8;
            sb.Begin();
        }

        #region 世界显示初始化

        private void InitializePlayerGrid()
        {
            // 设置PlayerGrid的Background
            PlayerGrid.Background = Brushes.Transparent;

            // 清除现有子元素
            PlayerGrid.Children.Clear();

            // 创建ScrollViewer
            _playerScrollViewer = new ScrollViewer();
            _playerScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            _playerScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            _playerScrollViewer.ScrollChanged += PlayerScrollViewer_ScrollChanged;
            _playerScrollViewer.Padding = new Thickness(0, 0, 10, 0);

            // 设置ScrollViewer的样式，使其背景透明
            _playerScrollViewer.Background = Brushes.Transparent;
            _playerScrollViewer.BorderThickness = new Thickness(0);

            // 创建Canvas作为内容容器
            _playerCanvas = new Canvas();
            _playerCanvas.Width = 340;
            _playerCanvas.Background = Brushes.Transparent;
            _playerCanvas.PreviewMouseWheel += PlayerCanvas_PreviewMouseWheel;

            // 设置初始高度
            _playerCanvas.Height = 400;

            _playerScrollViewer.Content = _playerCanvas;
            PlayerGrid.Children.Add(_playerScrollViewer);

            // 存储Canvas引用
            PlayerGrid.Tag = _playerCanvas;
        }

        // 获取世界列表的Canvas（右侧）
        private Canvas GetWorldCanvas()
        {
            if (WorldGrid.Tag is Canvas canvas)
                return canvas;

            InitializeWorldGrid();
#pragma warning disable CS8603 // 可能返回 null 引用。
            return WorldGrid.Tag as Canvas;
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        #endregion

        #region 玩家显示初始化

        private void InitializeWorldGrid()
        {
            // 设置WorldGrid的Background
            WorldGrid.Background = Brushes.Transparent;

            // 清除现有子元素
            WorldGrid.Children.Clear();

            // 创建ScrollViewer
            _worldScrollViewer = new ScrollViewer();
            _worldScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            _worldScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            _worldScrollViewer.ScrollChanged += WorldScrollViewer_ScrollChanged;
            _worldScrollViewer.Padding = new Thickness(0, 0, 10, 0);

            // 设置ScrollViewer的样式，使其背景透明
            _worldScrollViewer.Background = Brushes.Transparent;
            _worldScrollViewer.BorderThickness = new Thickness(0);

            // 创建Canvas作为内容容器
            _worldCanvas = new Canvas();
            _worldCanvas.Width = 340;
            _worldCanvas.Background = Brushes.Transparent;
            _worldCanvas.PreviewMouseWheel += WorldCanvas_PreviewMouseWheel;

            // 设置初始高度
            _worldCanvas.Height = 400;

            _worldScrollViewer.Content = _worldCanvas;
            WorldGrid.Children.Add(_worldScrollViewer);

            // 存储Canvas引用
            WorldGrid.Tag = _worldCanvas;
        }

        // 获取玩家列表的Canvas（左侧）
        private Canvas GetPlayerCanvas()
        {
            if (PlayerGrid.Tag is Canvas canvas)
                return canvas;

            InitializePlayerGrid();
#pragma warning disable CS8603 // 可能返回 null 引用。
            return PlayerGrid.Tag as Canvas;
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        #endregion

        #region 滚动控制

        private void PlayerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 可以在这里添加滑块同步逻辑（如果需要的话）
        }

        private void WorldScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 可以在这里添加滑块同步逻辑（如果需要的话）
        }

        private void PlayerCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_playerScrollViewer != null)
            {
                double newOffset = _playerScrollViewer.VerticalOffset - (e.Delta / 3);
                newOffset = Math.Max(0, Math.Min(_playerScrollViewer.ScrollableHeight, newOffset));
                _playerScrollViewer.ScrollToVerticalOffset(newOffset);
                e.Handled = true;
            }
        }

        private void WorldCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_worldScrollViewer != null)
            {
                double newOffset = _worldScrollViewer.VerticalOffset - (e.Delta / 3);
                newOffset = Math.Max(0, Math.Min(_worldScrollViewer.ScrollableHeight, newOffset));
                _worldScrollViewer.ScrollToVerticalOffset(newOffset);
                e.Handled = true;
            }
        }

        private void UpdateCanvasHeight()
        {
            // 更新Canvas高度，使其能够容纳所有卡片
            if (_playerCanvas != null)
            {
                if (playerCards.Count > 0)
                {
                    double totalHeight = playerNextY + 80; // 最后一个卡片的位置 + 卡片高度
                    _playerCanvas.Height = Math.Max(400, totalHeight);
                }
                else
                {
                    _playerCanvas.Height = 400;
                }
            }

            if (_worldCanvas != null)
            {
                if (worldCards.Count > 0)
                {
                    double totalHeight = worldNextY + 80;
                    _worldCanvas.Height = Math.Max(400, totalHeight);
                }
                else
                {
                    _worldCanvas.Height = 400;
                }
            }
        }

        private void RefreshScrollViewer()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_playerScrollViewer != null)
                {
                    _playerScrollViewer.UpdateLayout();
                    // 强制重新计算滚动范围
                    _playerScrollViewer.ScrollToVerticalOffset(0);
                }

                if (_worldScrollViewer != null)
                {
                    _worldScrollViewer.UpdateLayout();
                    _worldScrollViewer.ScrollToVerticalOffset(0);
                }
            }), DispatcherPriority.Render);
        }

        #endregion

        #region 世界卡片创建和管理

        public class WorldCardItem
        {
            public Border Border { get; set; }
            public string WorldName { get; set; }
            public string Directory { get; set; }
            public bool IsSelected { get; set; }

            private SolidColorBrush NormalBackground { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            private SolidColorBrush SelectedBackground { get; set; } = new SolidColorBrush(Color.FromRgb(200, 230, 255));

            public WorldCardItem(Border border, string worldName, string directory)
            {
                Border = border;
                WorldName = worldName;
                Directory = directory;
                IsSelected = false;
            }

            public void SetSelected(bool selected)
            {
                IsSelected = selected;
                Border.Background = selected ? SelectedBackground : NormalBackground;
            }

            public void ToggleSelected()
            {
                SetSelected(!IsSelected);
            }
        }

        private void LoadWorldData(WorldDirectoriesNameList data)
        {
            ClearAllWorlds();

            int count = Math.Min(data.world_directory_list.Count, data.world_name_list.Count);
            worldNextY = 50;

            for (int i = 0; i < count; i++)
            {
                CreateWorldCard(data.world_name_list[i], data.world_directory_list[i]);
            }

            // 更新Canvas高度（使用Dispatcher延迟执行，确保所有卡片都已添加）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateCanvasHeight();
                RefreshScrollViewer();
            }), DispatcherPriority.Loaded);
        }


        private void CreateWorldCard(string name, string directory)
        {
            Canvas worldCanvas = GetWorldCanvas();
            if (worldCanvas == null) return;

            Border border = new Border();
            border.Width = 320;
            border.Height = 80;
            border.CornerRadius = new CornerRadius(12);
            border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            border.BorderBrush = new SolidColorBrush(Colors.Transparent);
            border.BorderThickness = new Thickness(1.5);
            Canvas.SetLeft(border, 10);

            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 6;
            shadow.ShadowDepth = 2;
            shadow.Color = Colors.Black;
            shadow.Opacity = 0.3;
            border.Effect = shadow;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(8);
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            TextBlock nameText = new TextBlock();
            nameText.Text = name;
            nameText.FontSize = 14;
            nameText.FontWeight = FontWeights.Bold;
            nameText.Foreground = Brushes.Black;
            nameText.TextWrapping = TextWrapping.Wrap;
            nameText.TextAlignment = TextAlignment.Center;
            nameText.Margin = new Thickness(0, 0, 0, 5);

            TextBlock dirText = new TextBlock();
            string displayPath = directory;
            if (displayPath.Length > 64)
            {
                displayPath = displayPath.Substring(0, 64) + "...";
            }
            dirText.Text = displayPath;
            dirText.FontSize = 9;
            dirText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            dirText.TextWrapping = TextWrapping.Wrap;
            dirText.TextAlignment = TextAlignment.Center;

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(dirText);

            border.Child = stackPanel;

            Canvas.SetLeft(border, 10);
            Canvas.SetTop(border, worldNextY);

            worldCanvas.Children.Add(border);

            var cardItem = new WorldCardItem(border, name, directory);
            worldCards.Add(cardItem);

            border.MouseLeftButtonDown += (s, e) => OnWorldCardSelected(cardItem);
            border.MouseRightButtonDown += (s, e) => OnWorldCardRightClick(cardItem, e);
            border.MouseEnter += Border_MouseEnter;
            border.MouseLeave += Border_MouseLeave;

            PlayEntranceAnimation(border);

            worldNextY += 90;
        }

        private void OnWorldCardSelected(WorldCardItem selectedCard)
        {
            if (_selectedWorldCard != null && _selectedWorldCard != selectedCard)
            {
                _selectedWorldCard.SetSelected(false);
            }

            selectedCard.ToggleSelected();
            _selectedWorldCard = selectedCard.IsSelected ? selectedCard : null;

            Debug.WriteLine($"选中世界: {(selectedCard.IsSelected ? selectedCard.WorldName : "无")}");
        }

        private void OnWorldCardRightClick(WorldCardItem cardItem, MouseButtonEventArgs e)
        {
            OnWorldCardSelected(cardItem);
            ShowWorldContextMenu(cardItem, e.GetPosition(this));
            e.Handled = true;
        }

        private void ShowWorldContextMenu(WorldCardItem cardItem, Point position)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem enterWorldItem = new MenuItem { Header = "查看存档" };
            enterWorldItem.Click += (s, e) => OnEnterWorld(cardItem);

            MenuItem copyNameItem = new MenuItem { Header = "复制世界名称" };
            copyNameItem.Click += (s, e) => CopyToClipboard(cardItem.WorldName);

            MenuItem copyPathItem = new MenuItem { Header = "复制世界路径" };
            copyPathItem.Click += (s, e) => CopyToClipboard(cardItem.Directory);

            MenuItem openPathItem = new MenuItem { Header = "打开世界文件夹" };
            openPathItem.Click += (s, e) => OpenFolder(cardItem.Directory);

            MenuItem deleteWorldItem = new MenuItem { Header = "删除世界中的数据" };
            deleteWorldItem.Foreground = Brushes.Red;
            deleteWorldItem.Click += async (s, e) => await OnDeleteWorld(cardItem);

            contextMenu.Items.Add(enterWorldItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(copyNameItem);
            contextMenu.Items.Add(copyPathItem);
            contextMenu.Items.Add(openPathItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteWorldItem);

            contextMenu.IsOpen = true;
        }

        private void OnEnterWorld(WorldCardItem cardItem)
        {
            Debug.WriteLine($"进入世界: {cardItem.WorldName}");
            MessageBox.Show($"正在进入世界: {cardItem.WorldName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task OnDeleteWorld(WorldCardItem cardItem)
        {
            var result = MessageBox.Show(
                $"确定要删除世界 \"{cardItem.WorldName}\" 中所有玩家数据吗？\n此操作可以撤销！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Debug.WriteLine($"删除世界: {cardItem.WorldName}");

                bool success = await SendCommandAndWaitAsync(Command.DEL_WORLD, cardItem.Directory);
                if (success)
                {
                    SendCommand(Command.LIST_WORLD);
                }
                MessageBox.Show($"世界 \"{cardItem.WorldName}\" 已删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAllWorlds()
        {
            Canvas worldCanvas = GetWorldCanvas();
            if (worldCanvas == null) return;

            foreach (var cardItem in worldCards)
            {
                PlayExitAnimation(cardItem.Border);
            }

            worldCards.Clear();
            _selectedWorldCard = null;
            worldNextY = 50;

            // 重置Canvas高度
            if (_worldCanvas != null)
            {
                _worldCanvas.Height = 400;
            }
        }

        public WorldCardItem? GetSelectedWorld()
        {
            return _selectedWorldCard;
        }

        #endregion

        #region 玩家卡片创建和管理

        public class PlayerCardItem
        {
            public Border Border { get; set; }
            public UserInfo UserInfo { get; set; }
            public bool IsSelected { get; set; }

            private SolidColorBrush NormalBackground { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            private SolidColorBrush SelectedBackground { get; set; } = new SolidColorBrush(Color.FromRgb(200, 230, 255));

            public PlayerCardItem(Border border, UserInfo userInfo)
            {
                Border = border;
                UserInfo = userInfo;
                IsSelected = false;
            }

            public void SetSelected(bool selected)
            {
                IsSelected = selected;
                Border.Background = selected ? SelectedBackground : NormalBackground;
            }

            public void ToggleSelected()
            {
                SetSelected(!IsSelected);
            }
        }

        private void LoadPlayerData(List<UserInfo> players)
        {
            ClearAllPlayers();

            playerNextY = 50;

            for (int i = 0; i < players.Count; i++)
            {
                CreatePlayerCard(players[i]);
            }

            // 更新Canvas高度
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateCanvasHeight();
                RefreshScrollViewer();
            }), DispatcherPriority.Loaded);
        }

        private void CreatePlayerCard(UserInfo player)
        {
            Canvas playerCanvas = GetPlayerCanvas();
            if (playerCanvas == null) return;

            Border border = new Border();
            border.Width = 320;
            border.Height = 80;
            border.CornerRadius = new CornerRadius(12);
            border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            border.BorderBrush = new SolidColorBrush(Colors.White);
            border.BorderThickness = new Thickness(1.5);
            Canvas.SetLeft(border, 10);

            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 6;
            shadow.ShadowDepth = 2;
            shadow.Color = Colors.Black;
            shadow.Opacity = 0.3;
            border.Effect = shadow;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(8);
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            TextBlock nameText = new TextBlock();
            nameText.Text = player.user_name;
            nameText.FontSize = 14;
            nameText.FontWeight = FontWeights.Bold;
            nameText.Foreground = Brushes.Black;
            nameText.TextWrapping = TextWrapping.Wrap;
            nameText.TextAlignment = TextAlignment.Center;
            nameText.Margin = new Thickness(0, 0, 0, 5);

            TextBlock uuidText = new TextBlock();
            string displayUuid = player.uuid;
            if (displayUuid.Length > 64)
            {
                displayUuid = displayUuid.Substring(0, 61) + "...";
            }
            uuidText.Text = $"UUID: {displayUuid}";
            uuidText.FontSize = 9;
            uuidText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            uuidText.TextWrapping = TextWrapping.Wrap;
            uuidText.TextAlignment = TextAlignment.Center;
            uuidText.Margin = new Thickness(0, 0, 0, 3);

            TextBlock expireText = new TextBlock();
            string displayExpire = player.expiresOn;
            if (displayExpire.Length > 32)
            {
                displayExpire = displayExpire.Substring(0, 29) + "...";
            }
            expireText.Text = $"过期: {displayExpire}";
            expireText.FontSize = 9;
            expireText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            expireText.TextWrapping = TextWrapping.Wrap;
            expireText.TextAlignment = TextAlignment.Center;

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(uuidText);
            stackPanel.Children.Add(expireText);

            border.Child = stackPanel;

            Canvas.SetLeft(border, 10);
            Canvas.SetTop(border, playerNextY);

            playerCanvas.Children.Add(border);

            var cardItem = new PlayerCardItem(border, player);
            playerCards.Add(cardItem);

            border.MouseLeftButtonDown += (s, e) => OnPlayerCardSelected(cardItem);
            border.MouseRightButtonDown += (s, e) => OnPlayerCardRightClick(cardItem, e);
            border.MouseEnter += Border_MouseEnter;
            border.MouseLeave += Border_MouseLeave;

            PlayEntranceAnimation(border);

            playerNextY += 90;
        }

        private void OnPlayerCardSelected(PlayerCardItem selectedCard)
        {
            if (_selectedPlayerCard != null && _selectedPlayerCard != selectedCard)
            {
                _selectedPlayerCard.SetSelected(false);
            }

            selectedCard.ToggleSelected();
            _selectedPlayerCard = selectedCard.IsSelected ? selectedCard : null;

            Debug.WriteLine($"选中玩家: {(selectedCard.IsSelected ? selectedCard.UserInfo.user_name : "无")}");
        }

        private void OnPlayerCardRightClick(PlayerCardItem cardItem, MouseButtonEventArgs e)
        {
            OnPlayerCardSelected(cardItem);
            ShowPlayerContextMenu(cardItem, e.GetPosition(this));
            e.Handled = true;
        }

        private void ShowPlayerContextMenu(PlayerCardItem cardItem, Point position)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem copyNameItem = new MenuItem { Header = "复制玩家名称" };
            copyNameItem.Click += (s, e) => CopyToClipboard(cardItem.UserInfo.user_name);

            MenuItem copyUuidItem = new MenuItem { Header = "复制UUID" };
            copyUuidItem.Click += (s, e) => CopyToClipboard(cardItem.UserInfo.uuid);

            MenuItem viewInfoItem = new MenuItem { Header = "查看详细信息" };
            viewInfoItem.Click += (s, e) => ShowPlayerDetails(cardItem);

            MenuItem kickPlayerItem = new MenuItem { Header = "删除玩家数据" };
            kickPlayerItem.Foreground = Brushes.Red;
            kickPlayerItem.Click += async (s, e) => await OnKickPlayer(cardItem);

            contextMenu.Items.Add(copyNameItem);
            contextMenu.Items.Add(copyUuidItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(viewInfoItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(kickPlayerItem);

            contextMenu.IsOpen = true;
        }

        private void ShowPlayerDetails(PlayerCardItem cardItem)
        {
            Debug.WriteLine($"查看玩家详情: {cardItem.UserInfo.user_name}");
            MessageBox.Show(
                $"玩家名称: {cardItem.UserInfo.user_name}\n" +
                $"UUID: {cardItem.UserInfo.uuid}\n" +
                $"过期时间: {cardItem.UserInfo.expiresOn}",
                "玩家详细信息",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async Task OnKickPlayer(PlayerCardItem cardItem)
        {
            var result = MessageBox.Show(
                $"确定要删除玩家 \"{cardItem.UserInfo.user_name}\" 的全部数据吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Debug.WriteLine($"删除玩家: {cardItem.UserInfo.user_name}");
                bool success = await SendCommandAndWaitAsync(Command.DEL_PLAYER, cardItem.UserInfo.user_name);
                if (success)
                {
                    MessageBox.Show($"玩家 \"{cardItem.UserInfo.user_name}\" 已删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"玩家 \"{cardItem.UserInfo.user_name}\" 已删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                Debug.WriteLine($"已复制到剪贴板: {text}");
                MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制失败: {ex.Message}");
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    Debug.WriteLine($"路径不存在: {path}");
                    MessageBox.Show($"路径不存在: {path}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开文件夹失败: {ex.Message}");
                MessageBox.Show($"打开文件夹失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAllPlayers()
        {
            Canvas playerCanvas = GetPlayerCanvas();
            if (playerCanvas == null) return;

            foreach (var cardItem in playerCards)
            {
                PlayExitAnimation(cardItem.Border);
            }

            playerCards.Clear();
            _selectedPlayerCard = null;
            playerNextY = 50;

            // 重置Canvas高度
            if (_playerCanvas != null)
            {
                _playerCanvas.Height = 400;
            }
        }

        public PlayerCardItem? GetSelectedPlayer()
        {
            return _selectedPlayerCard;
        }

        #endregion

        #region 动画效果

        private void PlayEntranceAnimation(FrameworkElement element)
        {
            TranslateTransform translateTransform = new TranslateTransform(0, 30);
            element.RenderTransform = translateTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.Opacity = 0;

            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(50)
            };

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Tick += (s, args) =>
            {
                translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
                element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                timer.Stop();
            };
            timer.Start();
        }

        private void PlayExitAnimation(FrameworkElement element)
        {
            TranslateTransform translateTransform = new TranslateTransform(0, 0);
            element.RenderTransform = translateTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                From = 0,
                To = 80,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            translateYAnimation.Completed += (s, e) =>
            {
                Canvas worldCanvas = GetWorldCanvas();
                worldCanvas?.Children.Remove(element);
            };

            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
            element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(150),
                    AutoReverse = true
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

                if (border.Effect is DropShadowEffect shadow)
                {
                    DoubleAnimation blurAnimation = new DoubleAnimation
                    {
                        To = 12,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
                }
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Effect is DropShadowEffect shadow)
                {
                    DoubleAnimation blurAnimation = new DoubleAnimation
                    {
                        To = 6,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
                }
            }
        }

        #endregion

        #region 共享内存初始化

        private void InitializeLauncher()
        {
            var config = new Smc
            {
                EnableVerboseLogging = true,
                InitTimeout = 30000,
                ReplyTimeout = 5000
            };

            _launcher = new SharedMemoryLauncher(config);

            _launcher.ReplyReceived += OnReplyReceived;
            _launcher.ErrorOccurred += OnErrorOccurred;
            _launcher.OutputReceived += OnOutputReceived;
            _launcher.ProgramStatusChanged += OnProgramStatusChanged;
            _launcher.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void SetupStatusTimer()
        {
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _statusTimer.Tick += (s, e) => UpdateStatusDisplay();
            _statusTimer.Start();
        }

        private void UpdateStatusDisplay()
        {
            // 状态更新逻辑
        }

        private async System.Threading.Tasks.Task InitializeOnStartup()
        {
            bool success = await LaunchAsync();

            if (success)
            {
                Debug.WriteLine("共享内存初始化成功");
                SendCommand(Command.LIST_WORLD);
                SendCommand(Command.LIST_PLAYER);
            }
            else
            {
                Debug.WriteLine("共享内存初始化失败");
            }
        }

        #endregion

        #region 事件处理

        private void OnOutputReceived(object? sender, SharedMemoryFunc.OutputReceivedEventArgs e)
        {
            // 处理C++输出
        }

        private void OnReplyReceived(object? sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Title != "")
                {
                    Path.Text = e.Title;
                }

                switch (e.DataType)
                {
                    case StructDataType.WDNL:
                        if (e.Data != null)
                        {
                            WorldDirectoriesNameList wdnl = WorldDirectoriesNameList.FromBytes(e.Data);
                            Debug.WriteLine($"收到世界数据: 共 {wdnl.world_name_list.Count} 个世界");
                            LoadWorldData(wdnl);
                            flish = true;
                            _listWorldCompletionSource?.TrySetResult(true);
                        }
                        break;

                    case StructDataType.UI:
                        if (e.Data != null)
                        {
                            List<UserInfo> users = UserInfoListSerializer.FromBytes(e.Data);
                            Debug.WriteLine($"收到玩家数据: 共 {users.Count} 个玩家");
                            LoadPlayerData(users);
                            flish = true;
                            _listPlayerCompletionSource?.TrySetResult(true);
                        }
                        break;

                    case StructDataType.PIWIL:
                        if (e.Data != null)
                        {
                            PlayerInWorldInfoList piwil = PlayerInWorldInfoList.FromBytes(e.Data);
                            Debug.WriteLine($"收到玩家在世界信息数据");
                        }
                        break;
                }
            });
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"错误: {e.ErrorMessage}");
            });
        }

        private void OnProgramStatusChanged(object? sender, SharedMemoryFunc.ProgramStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"程序状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        private void OnConnectionStatusChanged(object? sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"连接状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        #endregion

        #region 公共方法 - 供界面调用

        public async System.Threading.Tasks.Task<bool> LaunchAsync(string? args = null)
        {
            try
            {
                return await _launcher.LaunchAsync(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage1_InitializeSharedMemory()
        {
            try
            {
                return _launcher.Stage1_InitializeSharedMemory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段1异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage2_StartCppProcess(string? args = null)
        {
            try
            {
                return _launcher.Stage2_StartCppProcess(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段2异常: {ex.Message}");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> Stage3_WaitForCppReadyAsync()
        {
            try
            {
                return await _launcher.Stage3_WaitForCppReadyAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段3异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage4_StartReplyListener()
        {
            try
            {
                _launcher.Stage4_StartReplyListener();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段4异常: {ex.Message}");
                return false;
            }
        }

        public bool SendCommand(Command command, string additional = "")
        {
            try
            {
                return _launcher.SendCommand(command, additional);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令异常: {ex.Message}");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> SendCommandAndWaitAsync(Command command, string additional = "", int timeoutMs = 5000)
        {
            try
            {
                var result = await _launcher.SendCommandAndWaitAsync(command, additional, timeoutMs);
                return result.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令并等待异常: {ex.Message}");
                return false;
            }
        }

        public void Shutdown()
        {
            try
            {
                _launcher?.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理异常: {ex.Message}");
            }
        }

        public SharedMemoryLauncher GetLauncher() => _launcher;

        public bool IsRunning => _launcher?.IsRunning ?? false;

        public ConnectStatus ConnectStatus => _launcher?.ConnectStatus ?? ConnectStatus.NOT_INITIALIZED;

        public ProgramStatus ProgramStatus => _launcher?.ProgramStatus ?? ProgramStatus.STOP;

        #endregion

        #region 清理

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Stop();
            _launcher?.Dispose();
            base.OnClosed(e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Storyboard storyboard = (Storyboard)this.Resources["Closing"];
            storyboard.Begin(this);
            _launcher?.Dispose();
        }

        #endregion

        #region 按钮事件

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            Back.IsEnabled = true;
            OK.IsEnabled = false;
            Choose.IsEnabled = false;

            Path.Text = "";

            bs = ButtonStatic.About;
            Storyboard sb = (Storyboard)this.Resources["AboutIntor"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            switch (bs)
            {
                case ButtonStatic.About:
                    {
                        Back.IsEnabled = false;
                        button.IsEnabled = true;
                        OK.IsEnabled = true;
                        Choose.IsEnabled = true;

                        Storyboard sb = (Storyboard)this.Resources["AboutIntorB"];

                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(0.1);
                        timer.Tick += (s, args) =>
                        {
                            sb.Begin(this);
                            timer.Stop();
                        };
                        timer.Start();
                        break;
                    }
                case ButtonStatic.Level2:
                    {
                        Back.IsEnabled = false;
                        button.IsEnabled = true;
                        OK.IsEnabled = true;
                        Choose.IsEnabled = true;

                        ClearAllPlayers();
                        ClearAllWorlds();

                        Storyboard sb = (Storyboard)this.Resources["Level2IntorB"];
                        sb.Begin();
                        break;
                    }

                default:
                    break;
            }
        }

        private async void OK_Click(object sender, RoutedEventArgs e)
        {
            if (Path.Text.Length > 0)
            {
                bs = ButtonStatic.Level2;

                Back.IsEnabled = true;
                OK.IsEnabled = false;
                Choose.IsEnabled = false;
                button.IsEnabled = false;

                Storyboard sb = (Storyboard)this.Resources["Level2Intor"];
                sb.Begin(this);
            }

            _listWorldCompletionSource = new TaskCompletionSource<bool>();
            _listPlayerCompletionSource = new TaskCompletionSource<bool>();

            SendCommand(Command.LIST_WORLD);
            await _listWorldCompletionSource.Task;

            SendCommand(Command.LIST_PLAYER);
        }

        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog open = new OpenFolderDialog();
            open.ShowDialog();
            Path.Text = open.FolderName;

            SendCommand(Command.M_SET_PATH, Path.Text.ToString());
        }

        #endregion
    }
}