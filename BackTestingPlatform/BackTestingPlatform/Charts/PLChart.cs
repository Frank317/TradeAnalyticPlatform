﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;
using NLog;
using BackTestingPlatform.Utilities;
using System.Collections.Generic;
using BackTestingPlatform.Model.Common;
using BackTestingPlatform.DataAccess.Stock;
using Autofac;
using BackTestingPlatform.Core;
using BackTestingPlatform.Utilities.Common;

namespace BackTestingPlatform.Charts
{
    public partial class PLChart : Form
    {
        private ZedGraphControl zedG;

        //Image对象，保存图片使用
        private Image imageZed;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        //PL曲线输入字典
        private Dictionary<string, double[]> lineChart = new Dictionary<string, double[]>();
        private string[] date = { };

        public PLChart(Dictionary<string, double[]> line, string[] datePeriod)
        {
            InitializeComponent();
            lineChart = line;
            date = datePeriod;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            //显示的属性设置，后期还要做美工处理**************
            zedG = new ZedGraphControl();
            SuspendLayout();
            // 图片属性设置 
            zedG.IsShowPointValues = false;
            zedG.Location = new Point(0, 0);
            zedG.Name = "zedG";
            zedG.PointValueFormat = "G";
            zedG.Size = new Size(1360, 764);
            zedG.TabIndex = 0;
            // Form属性设置
            AutoScaleBaseSize = new Size(10, 24);
            ClientSize = new Size(923, 538);
            Controls.Add(zedG);
            Name = "Form1";
            Text = "Form1";
            Load += new EventHandler(Form_Load);
            ResumeLayout(false);
        }

        private void Form_Load(object sender, EventArgs e)
        {
            MasterPane myPaneMaster = zedG.MasterPane;
            myPaneMaster.Title.Text = "NetWorth";
            myPaneMaster.Title.FontSpec.FontColor = Color.Black;

            GraphPane myPane = zedG.GraphPane;
            myPaneMaster.PaneList[0] = (myPane);

      //    //画一张的小图
      //    GraphPane paneStats = new GraphPane(new Rectangle(10, 10, 10, 10), "Mes", " t ( h )", "Rate");
      //    myPaneMaster.PaneList.Add(paneStats);

            LineItem[] myCurve = new LineItem[lineChart.Count];

            //建立indexD变量，索引myCurve变量
            int indexD = 0;
            //建立Random变量用于控制颜色变化
            Random aa = new Random();
            foreach (var variety in lineChart)
            {
                myCurve[indexD] = myPane.AddCurve(variety.Key, null, lineChart[variety.Key],
                    Color.FromArgb(aa.Next(1, 255), aa.Next(1, 255), aa.Next(1, 255)), SymbolType.None);
                myCurve[indexD].Symbol.Size = 8.0F;
                myCurve[indexD].Symbol.Fill = new Fill(Color.White);
                myCurve[indexD].Line.Width = 2.0F;
                ++indexD;
            }

            // Draw the X tics between the labels instead of at the labels
            //myPane.XAxis.IsTicsBetweenLabels = true;

            // Set the XAxis labels
            myPane.XAxis.Scale.TextLabels = date;
            // Set the XAxis to Text type
            myPane.XAxis.Type = AxisType.Text;

            //设置X轴和Y轴的名称
            myPane.XAxis.Title.Text = "时间";//X轴
            myPane.YAxis.Title.Text = "净值";//Y轴

            //设置图的title
            myPane.Title.Text = "净值曲线";

            // Fill the axis area with a gradient
            //myPane.AxisFill = new Fill(Color.White,
            //Color.FromArgb(255, 255, 166), 90F);
            // Fill the pane area with a solid color
            //myPane.PaneFill = new Fill(Color.FromArgb(250, 250, 255));

            //绩效指标图统计

            zedG.AxisChange();
            imageZed = zedG.GetImage();
        }

        public void SaveZed(string path)
        {
            imageZed.Save(path);
        }
    }
}
