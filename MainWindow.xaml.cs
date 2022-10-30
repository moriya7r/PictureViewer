using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PictureViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileList FileList { get; set; } = new FileList();
        private string Uri { get; set; } = "";
        private int Index { get; set; } = 0;
        private int RotationCount { get; set; } = 0;
        private int RotationCountOrig { get; set; } = 0;
        private byte Orientation { get; set; } = 0;
        private FileStream FStream { get; set; }
        private bool fsflag { get; set; } = false;
        private readonly byte[] RotationSeq = { 1, 6, 3, 8 };
        private readonly byte[] RotationFlipSeq = { 2, 5, 4, 7 };

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            BackgroundColorSetting();
            if (App.CommandLineString != "")
            {
                Uri = App.CommandLineString;
                PictureUpdate();
                DirectoryUpdate();
                FileList.SortbyNameAsc();
                Index = FileList.GetIndex(Uri);
                leftButton.IsEnabled = true;
                rightButton.IsEnabled = true;
                counterclockwiseButton.IsEnabled = true;
                clockwiseButton.IsEnabled = true;
                sortButton.IsEnabled = true;
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Uri = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                PictureUpdate();
                DirectoryUpdate();
                FileList.SortbyNameAsc();
                Index = FileList.GetIndex(Uri);
                leftButton.IsEnabled = true;
                rightButton.IsEnabled = true;
                counterclockwiseButton.IsEnabled = true;
                clockwiseButton.IsEnabled = true;
                sortButton.IsEnabled = true;
                nameAsc.IsChecked = true;
                nameDesc.IsChecked = false;
                dateAsc.IsChecked = false;
                dateDesc.IsChecked = false;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    PreviousFile();
                    break;
                case Key.Right:
                    NextFile();
                    break;
                case Key.Delete:
                    if (FileList.Count != 0)
                    {
                        int deleteidx = Index;
                        if (MessageBox.Show("削除しますか？", "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                FStream.Close();
                                FileSystem.DeleteFile(FileList.GetFileName(deleteidx), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            NextFile();
                        }
                    }
                    break;
                case Key.Escape:
                    FullScreenRelease();
                    break;
                default:
                    break;
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            FullScreenRelease();
        }

        private void PictureBox1_DoubleClick(object sender, EventArgs e)
        {
            FullScreenExecution();
        }

        private void leftButton_Click(object sender, RoutedEventArgs e)
        {
            PreviousFile();
        }

        private void rightButton_Click(object sender, RoutedEventArgs e)
        {
            NextFile();
        }

        private void counterclockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            ImageRotation(System.Drawing.RotateFlipType.Rotate270FlipNone);
            RotationCount--;
            if (RotationCount < 0)
            {
                RotationCount = 3;
            }
            if (Orientation != 0 && RotationCount != RotationCountOrig)
            {
                saveButton.IsEnabled = true;
            }
            else
            {
                saveButton.IsEnabled = false;
            }
        }

        private void clockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            ImageRotation(System.Drawing.RotateFlipType.Rotate90FlipNone);
            RotationCount++;
            if (RotationCount > 3)
            {
                RotationCount = 0;
            }
            if (Orientation != 0 && RotationCount != RotationCountOrig)
            {
                saveButton.IsEnabled = true;
            }
            else
            {
                saveButton.IsEnabled = false;
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("回転情報(Exif)を保存しますか？", "保存確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int idx = Array.IndexOf(RotationSeq, Orientation);
                byte ori = 0;
                if (idx >= 0)
                {
                    ori = RotationSeq[RotationCount % 4];
                }
                else
                {
                    idx = Array.IndexOf(RotationFlipSeq, Orientation);
                    if (idx >= 0)
                    {
                        ori = RotationFlipSeq[RotationCount % 4];
                    }
                }
                if (ori != 0)
                {
                    System.Drawing.Imaging.PropertyItem property = PictureBox1.Image.PropertyItems.FirstOrDefault(p => p.Id == 0x0112);
                    if (property != null)
                    {
                        property.Value[0] = ori;
                        PictureBox1.Image.SetPropertyItem(property);
                        PictureBox1.Visible = false;
                        for (int i = RotationCount; i > 0; i--)
                        {
                            ImageRotation(System.Drawing.RotateFlipType.Rotate270FlipNone);
                        }
                        FStream.Close();
                        PictureBox1.Image.Save(Uri, System.Drawing.Imaging.ImageFormat.Jpeg);
                        PictureUpdate();
                        PictureBox1.Visible = true;
                    }
                }
            }
        }

        private void sortButton_Click(object sender, RoutedEventArgs e)
        {
            sortButton.ContextMenu.IsOpen = true;
        }

        private void nameAsc_Click(object sender, RoutedEventArgs e)
        {
            nameAsc.IsChecked = true;
            nameDesc.IsChecked = false;
            dateAsc.IsChecked = false;
            dateDesc.IsChecked = false;
            DirectoryUpdate();
            FileList.SortbyNameAsc();
            Index = FileList.GetIndex(Uri);
        }

        private void nameDesc_Click(object sender, RoutedEventArgs e)
        {
            nameAsc.IsChecked = false;
            nameDesc.IsChecked = true;
            dateAsc.IsChecked = false;
            dateDesc.IsChecked = false;
            DirectoryUpdate();
            FileList.SortbyNameDesc();
            Index = FileList.GetIndex(Uri);
        }

        private void dateAsc_Click(object sender, RoutedEventArgs e)
        {
            nameAsc.IsChecked = false;
            nameDesc.IsChecked = false;
            dateAsc.IsChecked = true;
            dateDesc.IsChecked = false;
            DirectoryUpdate();
            FileList.SortbyDateAsc();
            Index = FileList.GetIndex(Uri);
        }

        private void dateDesc_Click(object sender, RoutedEventArgs e)
        {
            nameAsc.IsChecked = false;
            nameDesc.IsChecked = false;
            dateAsc.IsChecked = false;
            dateDesc.IsChecked = true;
            DirectoryUpdate();
            FileList.SortbyDateDesc();
            Index = FileList.GetIndex(Uri);
        }

        private void fullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            FullScreenExecution();
        }

        private void backgroundcolorButton_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.BackgroundWhite)
            {
                Properties.Settings.Default.BackgroundWhite = false;
                Properties.Settings.Default.Save();
                BackgroundColorSetting();
            }
            else
            {
                Properties.Settings.Default.BackgroundWhite = true;
                Properties.Settings.Default.Save();
                BackgroundColorSetting();
            }
        }

        private void PictureUpdate()
        {
            if (FStream != null)
            {
                FStream.Close();
            }
            BitmapImage bitmapImage = new BitmapImage();
            FStream = File.OpenRead(Uri);
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = FStream;
            bitmapImage.EndInit();
            PictureBox1.Image = System.Drawing.Image.FromStream(bitmapImage.StreamSource);
            PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            Title = System.IO.Path.GetFileName(Uri);

            RotationCount = 0;
            RotationCountOrig = 0;
            Orientation = 0;
            saveButton.IsEnabled = false;
            saveButton.Visibility = Visibility.Hidden;

            System.Drawing.Imaging.PropertyItem property = PictureBox1.Image.PropertyItems.FirstOrDefault(p => p.Id == 0x0112);
            if(property != null)
            {
                saveButton.Visibility = Visibility.Visible;
                Orientation = property.Value[0];
                switch (Orientation)
                {
                    case 1:
                        break;
                    case 2:
                        ImageRotation(System.Drawing.RotateFlipType.RotateNoneFlipX);
                        break;
                    case 3:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate180FlipNone);
                        RotationCount = 2;
                        RotationCountOrig = 2;
                        break;
                    case 4:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate180FlipX);
                        RotationCount = 2;
                        RotationCountOrig = 2;
                        break;
                    case 5:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate90FlipX);
                        RotationCount = 1;
                        RotationCountOrig = 1;
                        break;
                    case 6:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate90FlipNone);
                        RotationCount = 1;
                        RotationCountOrig = 1;
                        break;
                    case 7:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate270FlipX);
                        RotationCount = 3;
                        RotationCountOrig = 3;
                        break;
                    case 8:
                        ImageRotation(System.Drawing.RotateFlipType.Rotate270FlipNone);
                        RotationCount = 3;
                        RotationCountOrig = 3;
                        break;
                    default:
                        break;
                }
            }
        }

        private void DirectoryUpdate()
        {
            string[] ext = { ".jpg", ".jpeg", ".png", ".gif" };
            var path = System.IO.Path.GetDirectoryName(Uri);
            var nameList = new List<string>(Directory.GetFiles(path, "*")
                .Where(x => ext.Contains(System.IO.Path.GetExtension(x).ToLower())));
            FileList = new FileList(nameList);
            Index = FileList.GetIndex(Uri);
        }

        private void PreviousFile()
        {
            if (FileList.Count != 0)
            {
                Index--;
                if (Index < 0)
                {
                    Index = FileList.Count - 1;
                }
                Uri = FileList.GetFileName(Index);
                PictureUpdate();
            }
        }

        private void NextFile()
        {
            if (FileList.Count != 0)
            {
                Index++;
                if (Index >= FileList.Count)
                {
                    Index = 0;
                }
                Uri = FileList.GetFileName(Index);
                PictureUpdate();
            }
        }

        private void ImageRotation(System.Drawing.RotateFlipType rotateFlipType)
        {
            if (PictureBox1.Image != null)
            {
                PictureBox1.Image.RotateFlip(rotateFlipType);
                PictureBox1.Refresh();
            }
        }

        private void FullScreenExecution()
        {
            if (!fsflag)
            {
                fsflag = true;
                Grid1.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        private void FullScreenRelease()
        {
            if (fsflag)
            {
                fsflag = false;
                Grid1.RowDefinitions[1].Height = new GridLength(60);
            }
        }

        private void BackgroundColorSetting()
        {
            if (Properties.Settings.Default.BackgroundWhite)
            {
                PictureBox1.BackColor = System.Drawing.Color.White;
                backgroundcolorButton.Content = "🔲";
            }
            else
            {
                PictureBox1.BackColor = System.Drawing.Color.Black;
                backgroundcolorButton.Content = "⬛";
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.WindowMaximized = true;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.WindowMaximized = false;
                Properties.Settings.Default.Save();
            }
        }
    }

    public class FileList
    {
        private List<FileInfo> fileInfos { get; set; } = new List<FileInfo>();

        public FileList()
        {
        }
        public FileList(List<string> names)
        {
            foreach(string name in names)
            {
                FileInfo fileInfo = new FileInfo();
                fileInfo.FileName = name;
                fileInfo.DateTime = System.IO.File.GetLastWriteTime(name);
                fileInfos.Add(fileInfo);
            }
        }
        public int Count { get { return fileInfos.Count; } }
        public string GetFileName(int index)
        {
            return fileInfos[index].FileName;
        }
        public int GetIndex(string name)
        {
            return fileInfos.FindIndex(x => x.FileName == name);
        }
        public void SortbyNameAsc()
        {
            fileInfos = fileInfos.OrderBy(x => x.FileName, new LogicalStringComparer()).ToList();
        }
        public void SortbyNameDesc()
        {
            fileInfos = fileInfos.OrderBy(x => x.FileName, new LogicalStringComparer()).Reverse().ToList();
        }
        public void SortbyDateAsc()
        {
            fileInfos = fileInfos.OrderBy(x => x.DateTime).Reverse().ToList();
        }
        public void SortbyDateDesc()
        {
            fileInfos = fileInfos.OrderBy(x => x.DateTime).Reverse().ToList();
        }
    }

    public class FileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime DateTime { get; set; } = DateTime.MinValue;
    }

    /// <summary>
    /// 大文字小文字を区別せずに、
    /// 文字列内に含まれている数字を考慮して文字列を比較します。
    /// </summary>
    public class LogicalStringComparer :
        System.Collections.IComparer,
        System.Collections.Generic.IComparer<string>
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode,
            ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }

        public int Compare(object x, object y)
        {
            return this.Compare(x.ToString(), y.ToString());
        }
    }
}
