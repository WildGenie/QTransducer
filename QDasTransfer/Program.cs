﻿using System;
using System.Windows.Forms;
using QTrans;
using System.IO;
using System.Reflection; 

namespace QDasTransfer
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Control.CheckForIllegalCrossThreadCalls = false;

            values.transducer =  new QTrans.Company.Y2017.T201705_Zhiqi_3D();

            //启动主窗体。
            NewMainForm mf = new NewMainForm();
			Application.Run(mf); 


		}

         

	}
}