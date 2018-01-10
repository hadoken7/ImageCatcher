using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace ImageCatcher
{

    public partial class ScreenForm : Form
    {
        public ScreenForm()
        {
            InitializeComponent();
        }

        private string APP_ID = "9225632";
        private string API_KEY = "5vQwBZYcrD2pK45GMIukymnP";
        private string SECRET_KEY = "b0r5wu2snhmZpHuTpRX2ockhUNBHRbGP";


        private Baidu.Aip.Ocr.Ocr client = null;


        //public event copyToFatherTextBox copytoFather;  //截屏完毕后交个父窗体处理截图
        public bool begin = false;   //是否开始截屏
        public bool isWaitingDoubleClick = false;
        public Point firstPoint = new Point(0, 0);  //鼠标第一点
        public Point secondPoint = new Point(0, 0);  //鼠标第二点
        public Image cachImage = null;  //用来缓存截获的屏幕
        public int halfWidth = 0;//保存屏幕一半的宽度
        public int halfHeight = 0;//保存屏幕一般的高度

        /*复制整个屏幕,并让窗体填充屏幕*/
        public void copyScreen()
        {
            client = new Baidu.Aip.Ocr.Ocr(API_KEY, SECRET_KEY);
            Rectangle r = Screen.PrimaryScreen.Bounds;
            Image img = new Bitmap(r.Width, r.Height);
            Graphics g = Graphics.FromImage(img);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), r.Size);

            //窗体最大化，及相关处理
            this.Width = r.Width;
            this.Height = r.Height;
            this.Left = 0;
            this.Top = 0;
            pictureBox1.Width = r.Width;
            pictureBox1.Height = r.Height;
            pictureBox1.BackgroundImage = img;
            cachImage = img;
            halfWidth = r.Width / 2;
            halfHeight = r.Height / 2;
        }

        private void ScreenForm_Load(object sender, EventArgs e)
        {
            copyScreen();
        }

        /*鼠标按下时开始截图*/
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isWaitingDoubleClick)
            {
                begin = true;
                firstPoint = new Point(e.X, e.Y);
                changePoint(e.X, e.Y);
                label1.Visible = true;
            }
        }
        /*鼠标移动时显示截取区域的边框*/
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (begin)
            {
                //获取新的右下角坐标
                secondPoint = new Point(e.X, e.Y);
                int minX = Math.Min(firstPoint.X, secondPoint.X);
                int minY = Math.Min(firstPoint.Y, secondPoint.Y);
                int maxX = Math.Max(firstPoint.X, secondPoint.X);
                int maxY = Math.Max(firstPoint.Y, secondPoint.Y);

                //重新画背景图
                Image tempimage = new Bitmap(cachImage);
                Graphics g = Graphics.FromImage(tempimage);
                //画裁剪框
                g.DrawRectangle(new Pen(Color.Red), minX, minY, maxX - minX, maxY - minY);
                pictureBox1.Image = tempimage;
                //计算坐标信息
                label1.Text = "左上角坐标：(" + minX.ToString() + "," + minY.ToString() + ")\r\n";
                label1.Text += "右下角坐标：(" + maxX.ToString() + "," + maxY.ToString() + ")\r\n";
                label1.Text += "截图大小：" + (maxX - minX) + "×" + (maxY - minY) + "\r\n";
                label1.Text += "双击任意地方结束截屏！";
                changePoint((minX + maxX) / 2, (minY + maxY) / 2);
            }
        }
        /*动态调整显示信息的位置,输入参数为当前截屏鼠标位置*/
        public void changePoint(int x, int y)
        {
            if (x < halfWidth)
            {
                if (y < halfHeight)
                { label1.Top = halfHeight; label1.Left = halfWidth; }
                else
                { label1.Top = 0; label1.Left = halfWidth; }
            }
            else
            {
                if (y < halfHeight)
                { label1.Top = halfHeight; label1.Left = 0; }
                else
                { label1.Top = 0; label1.Left = 0; }
            }
        }

        /*鼠标放开时截图操作完成*/
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            begin = false;
            isWaitingDoubleClick = true; //之后再点击就是双击事件了
        }
        /*双击时截图时，通知父窗体完成截图操作，同时关闭本窗体*/
        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (firstPoint != secondPoint)
            {
                int minX = Math.Min(firstPoint.X, secondPoint.X);
                int minY = Math.Min(firstPoint.Y, secondPoint.Y);
                int maxX = Math.Max(firstPoint.X, secondPoint.X);
                int maxY = Math.Max(firstPoint.Y, secondPoint.Y);
                Rectangle r = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                Bitmap image_result = new Bitmap(cachImage).Clone(r,cachImage.PixelFormat);
                JObject baidu_result = client.GeneralBasic(Bitmap2Byte(image_result));
                String string_result = ParseBaiduResult(baidu_result);
                if (string_result != null && string_result.Length > 0)
                {
                    string url = "https://www.baidu.com/s?wd=" + string_result;
                    System.Diagnostics.Process.Start(url);
                } else
                {
                    MessageBox.Show("字符分析失败");
                }
            }
            this.Close();
        }

        private void CancelPaint()
        {
            isWaitingDoubleClick = false;
            begin = false;
            pictureBox1.Image = cachImage;
            label1.Visible = false;
        }

        private void CloseThisPaint()
        {
            this.Close();
        }

        private void FromKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                if (begin || isWaitingDoubleClick)
                {
                    CancelPaint();
                } else
                {
                    CloseThisPaint();
                }
            }
        }

        public static string ParseBaiduResult(JObject jObject)
        {
            if (jObject["words_result"] != null)
            {
                var names = from staff in jObject["words_result"].Children()
                            select (string)staff["words"];

                StringBuilder sb = new StringBuilder();
                foreach (var name in names)
                    sb.Append(name);
                return sb.ToString();
            }
            return null;
        }

        public static byte[] Bitmap2Byte(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                byte[] data = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, Convert.ToInt32(stream.Length));
                return data;
            }
        }
    }
}