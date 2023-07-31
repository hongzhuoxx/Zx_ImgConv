using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;

namespace ImgConv
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// 允许处理的格式范围
        /// </summary>
        List<string> FileExtensions = new List<string>()
        {
            ".bmp",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif"
        };

        /// <summary>
        /// 转换后的图像格式
        /// </summary>
        string TagImgType = "png";

        /// <summary>
        /// 是否删除原来的图片
        /// </summary>
        bool isDelSource = false;

        /// <summary>
        /// 是否遍历所有文件夹
        /// </summary>
        bool isTraverseAll = false;

        /// <summary>
        /// 构造
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            AddEvent();
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        private void AddEvent()
        {
            bt_SelectDir.Click += Bt_SelectDir_Click;
            btBeginConvImg.Click += btBeginConvImg_Click;
            btBeginSplit.Click += BtBeginSplit_Click;
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bt_SelectDir_Click(object sender, RoutedEventArgs e)
        {
            SelectDir();
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        public void SelectDir()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            tb_dirPath.Text = dialog.SelectedPath.Trim();
        }

        /// <summary>
        /// 开始处理格式转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void btBeginConvImg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_dirPath.Text))
            {
                MessageBox.Show("请选择文件夹");
                return;
            }


            if (!Directory.Exists(tb_dirPath.Text))
            {
                MessageBox.Show("文件夹不存在");
                return;
            }

            // 是否删除原文件
            this.isDelSource = IsDelSource?.IsChecked == true;
            // 是否遍历所有文件
            this.isTraverseAll = cbExAllFile?.IsChecked == true;

            // 禁用按钮
            this.btBeginConvImg.IsEnabled = false;

            bool _IsParallel = IsParallel.IsChecked == true;
            string path = tb_dirPath.Text;


            Task.Run(() =>
            {
                try
                {
                    if (_IsParallel)
                    {
                        TraverseAllImg(path, ImgConv);
                    }
                    else
                    {
                        TraverseAllImg_TSync(path, ImgConv);
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("处理完成");
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("出现错误:" + ex.Message);
                    }));
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.btBeginConvImg.IsEnabled = true;
                    }));
                }
            });
        }

        /// <summary>
        /// 分割图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void BtBeginSplit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_dirPath.Text))
            {
                MessageBox.Show("请选择文件夹");
                return;
            }


            if (!Directory.Exists(tb_dirPath.Text))
            {
                MessageBox.Show("文件夹不存在");
                return;
            }

            // 是否删除原文件
            this.isDelSource = IsDelSource?.IsChecked == true;
            // 是否遍历所有文件
            this.isTraverseAll = cbExAllFile?.IsChecked == true;

            // 禁用按钮
            this.btBeginSplit.IsEnabled = false;

            string path = tb_dirPath.Text;
            bool _IsParallel = IsParallel.IsChecked == true;

            int srow = 0;
            int scol = 0;


            try
            {
                srow = Convert.ToInt32(tb_SplitRows.Text);
                scol = Convert.ToInt32(tb_SplitCols.Text);

                if (srow <= 0 || scol <= 0)
                {
                    MessageBox.Show("请输入正确的分割数量");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("请输入正确的分割数量");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    if (_IsParallel)
                    {
                        TraverseAllImg(path, (fi, fm) =>
                        {
                            SplitAndSaveImage(fi, srow, scol, fm);
                        });
                    }
                    else
                    {
                        TraverseAllImg_TSync(path, (fi, fm) =>
                        {
                            SplitAndSaveImage(fi, srow, scol, fm);
                        });
                    }


                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("处理完成");
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("出现错误:" + ex.Message);
                    }));
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.btBeginSplit.IsEnabled = true;
                    }));
                }
            });
        }

        /// <summary>
        /// 便利所有文件进行处理 异步循环
        /// </summary>
        /// <param name="DirPath">文件夹</param>
        /// <param name="ExAction">处理方法</param>
        private void TraverseAllImg(string DirPath, Action<FileInfo, System.Drawing.Imaging.ImageFormat> ExAction)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(DirPath);

            System.Drawing.Imaging.ImageFormat tagImageFormat = StrToImageFormat(TagImgType);

            Parallel.ForEach(directoryInfo.GetFiles(), (item) =>
            {
                if (FileExtensions.Contains(item.Extension))
                {
                    if (item.Extension == ("." + TagImgType))
                    {
                        return;
                    }

                    ExAction(item, tagImageFormat);
                }
            });

            if (isTraverseAll)
            {
                Parallel.ForEach(directoryInfo.GetDirectories(), (item) =>
                {
                    TraverseAllImg(item.FullName, ExAction);
                });
            }

        }

        /// <summary>
        /// 便利所有文件进行处理 同步循环
        /// </summary>
        /// <param name="DirPath"></param>
        /// <param name="ExAction"></param>
        private void TraverseAllImg_TSync(string DirPath, Action<FileInfo, System.Drawing.Imaging.ImageFormat> ExAction)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(DirPath);

            System.Drawing.Imaging.ImageFormat tagImageFormat = StrToImageFormat(TagImgType);

            foreach (var item in directoryInfo.GetFiles())
            {
                if (FileExtensions.Contains(item.Extension))
                {
                    if (item.Extension == ("." + TagImgType))
                    {
                        return;
                    }

                    ExAction(item, tagImageFormat);
                }
            }

            if (isTraverseAll)
            {
                foreach (var item in directoryInfo.GetDirectories())
                {
                    TraverseAllImg_TSync(item.FullName, ExAction);
                }
            }

        }

        /// <summary>
        /// 格式转换
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="imageFormat"></param>
        private void ImgConv(FileInfo fileinfo, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            try
            {
                System.Drawing.Image bitmap = Bitmap.FromFile(fileinfo.FullName);
                string FileName = fileinfo.FullName.Replace(fileinfo.Extension.ToLower(), "." + imageFormat.ToString().ToLower());
                bitmap.Save(FileName, imageFormat);
                bitmap.Dispose();

                if (isDelSource)
                {
                    fileinfo.Delete();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// 分割并保存照片
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="imageFormat"></param>
        public void SplitAndSaveImage(FileInfo fileinfo, int rows, int cols, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            // Load the source image
            Bitmap sourceImage = new Bitmap(fileinfo.FullName);

            // Calculate the width and height of each cell
            int cellWidth = sourceImage.Width / cols;
            int cellHeight = sourceImage.Height / rows;

            // Loop through the cells and save each one as a separate image
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Create a new bitmap for this cell
                    Bitmap cellImage = new Bitmap(cellWidth, cellHeight);

                    // Copy the pixels from the source image to the cell image
                    for (int x = 0; x < cellWidth; x++)
                    {
                        for (int y = 0; y < cellHeight; y++)
                        {
                            System.Drawing.Color pixelColor = sourceImage.GetPixel(col * cellWidth + x, row * cellHeight + y);
                            cellImage.SetPixel(x, y, pixelColor);
                        }
                    }

                    string SaveFileName = fileinfo.FullName.Replace(fileinfo.Extension.ToLower(), "");
                    SaveFileName += $"cell_{rows}-{cols}_To_{row + 1}-{col + 1}" + "." + imageFormat.ToString().ToLower();

                    cellImage.Save(SaveFileName, imageFormat);
                }
            }

            sourceImage.Dispose();

            if (isDelSource)
            {
                fileinfo.Delete();
            }
        }

        /// <summary>
        /// 后缀名转图像格式
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        private System.Drawing.Imaging.ImageFormat StrToImageFormat(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                "png" => System.Drawing.Imaging.ImageFormat.Png,
                "jpg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                "jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                "gif" => System.Drawing.Imaging.ImageFormat.Gif,
                "bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };
        }

    }
}
