using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace ExportDll
{
	static class Class1
	{
		static void Main(params string[] arrStr)
		{
			try
			{
				//проверки
				if (arrStr.Length == 0)
				{
					Console.WriteLine("Должен быть указан путь к сборке!");
					Console.ReadKey();
					return;
				}
				FileInfo fi = new FileInfo(arrStr[0]);
				if (!fi.Exists)
				{
					Console.WriteLine("Указанная сборка не существует!");
					Console.ReadKey();
					return;
				}
				if (!fi.Extension.Equals(".dll"))
				{
					Console.WriteLine("Указанная сборка должна быть dll!");
					Console.ReadKey();
					return;
				}
				StreamReader paramsread = new StreamReader(Application.StartupPath + "\\Params.set");
				string setstr = paramsread.ReadToEnd();
				paramsread.Close();
				string ilasmpath = setstr.Substring(setstr.IndexOf("ilasmpath=", StringComparison.Ordinal) + 10,
																setstr.IndexOf("\r\n", setstr.IndexOf("ilasmpath=", StringComparison.Ordinal) + 10,
																					StringComparison.Ordinal) -
																(setstr.IndexOf("ilasmpath=", StringComparison.Ordinal) + 10));
				string ildasmpath = setstr.Substring(setstr.IndexOf("ildasmpath=", StringComparison.Ordinal) + 11,
																 setstr.IndexOf("\r\n",
																					 setstr.IndexOf("ildasmpath=", StringComparison.Ordinal) + 11,
																					 StringComparison.Ordinal) -
																 (setstr.IndexOf("ildasmpath=", StringComparison.Ordinal) + 11));
				if (!File.Exists(ilasmpath))
				{
					Console.WriteLine("Путь к ассемблеру неверен!");
					Console.ReadKey();
					return;
				}
				if (!File.Exists(ildasmpath))
				{
					Console.WriteLine("Путь к дизассемблеру неверен!");
					Console.ReadKey();
					return;
				}

				//дизассемблирование
				string ilName = fi.FullName;
				ilName = ilName.Substring(0, ilName.LastIndexOf(".", StringComparison.Ordinal)) + ".il";
				string cmd = " /utf8 /OUT=\"" + ilName + "\" \"" + fi.FullName + "\"";
				Process proc = new Process { StartInfo = new ProcessStartInfo(ildasmpath, cmd) };
				proc.Start();
				proc.WaitForExit();
				StreamReader read = new StreamReader(ilName);
				StringBuilder ilstr = new StringBuilder(read.ReadToEnd());
				read.Close();

				//обработка
				ilstr = ilstr.Replace(".corflags 0x00000001", ".corflags 0x00000002");
				int j = 0;
				for (int i = ilstr.ToString().IndexOf("System.Reflection.ObfuscationAttribute", 0, StringComparison.Ordinal); i != -1;
					i = ilstr.ToString().IndexOf("System.Reflection.ObfuscationAttribute", i, StringComparison.Ordinal))
				{
					j++;
					i = ilstr.ToString().IndexOf("// llExport\r\n", i, StringComparison.Ordinal) + 13;
					ilstr = ilstr.Insert(i, "    .export[" + j + "]\r\n");
				}
				Encoding enc = Encoding.UTF8;
				byte[] bt = enc.GetBytes(ilstr.ToString());
				using (FileStream fs = new FileStream(ilName, FileMode.Create))
				{
					fs.WriteByte(0xEF);
					fs.WriteByte(0xBB);
					fs.WriteByte(0xBF);
					fs.Write(bt, 0, bt.Length);
				}

				//сборка
				proc = new Process
				       	{
				       		StartInfo = new ProcessStartInfo(ilasmpath, " /DLL /OPTIMIZE /RESOURCE=\"" +
				       		                                            ilName.Substring(0,
				       		                                                             ilName.LastIndexOf(".",
				       		                                                                                StringComparison.Ordinal)) +
				       		                                            ".res" + "\" \"" + ilName + "\"")
				       	};
				proc.Start();
				proc.WaitForExit();
				File.Delete(ilName);
				File.Delete(ilName.Substring(0, ilName.LastIndexOf(".", StringComparison.Ordinal)) + ".res");
				DirectoryInfo di = new DirectoryInfo(fi.DirectoryName);
				FileInfo[] fiRes = di.GetFiles("*.resources");
				foreach (FileInfo fiR in fiRes)
					File.Delete(fiR.FullName);
			}
			catch (Exception ex)
			{
				StreamWriter write = new StreamWriter(Application.StartupPath + "\\Error.txt");
				write.WriteLine(ex);
				write.Close();
			}
		}
	}
}