using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Threading;
using System.Net;

namespace Bilibili_DownVideoToMp4
{
    class Program
    {
        //参考网址 https://www.52pojie.cn/thread-1061388-1-1.html
        public static Logger logger = LogManager.GetLogger("RunLog");


        public static string bilibiliDownloadPath = "";
        public static string outAllMp4Path = "";

        public static string isDirNameWithAvid = "";
        public static string isMp4FileNameWithPageId = "";
        public static string isAutoCalcSleepTmes = "";

        public static string sleepTimes = "";
        public static string isOnlyCreateBat = "";
        public static string isAutoCloseApp = "";
        public static string isAutoClearBeforeConvert = "";
        public static string serverChanKey = "";



        static void Main(string[] args)
        {

            LogGreen("-------------------------------------------------");
            LogGreen("Bilibili_DownVideoToMp4_V1.3");

            bilibiliDownloadPath = ConfigurationManager.AppSettings["bilibiliDownloadPath"].ToString();
            outAllMp4Path = ConfigurationManager.AppSettings["outAllMp4Path"].ToString();

            isDirNameWithAvid = ConfigurationManager.AppSettings["isDirNameWithAvid"].ToString();
            isMp4FileNameWithPageId = ConfigurationManager.AppSettings["isMp4FileNameWithPageId"].ToString();
            isAutoCalcSleepTmes = ConfigurationManager.AppSettings["isAutoCalcSleepTmes"].ToString();


            sleepTimes = ConfigurationManager.AppSettings["sleepTimes"].ToString();
            isOnlyCreateBat = ConfigurationManager.AppSettings["isOnlyCreateBat"].ToString();
            isAutoCloseApp = ConfigurationManager.AppSettings["isAutoCloseApp"].ToString();
            isAutoClearBeforeConvert = ConfigurationManager.AppSettings["isAutoClearBeforeConvert"].ToString();
            serverChanKey = ConfigurationManager.AppSettings["serverChanKey"].ToString();


            //配置文件参数容错处理
            Log("Bilibili下载的Download文件夹路径=" + bilibiliDownloadPath);
            Log("转换的MP4文件存放路径=" + outAllMp4Path);
            if (!Directory.Exists(outAllMp4Path))
            {
                Log("创建文件夹 " + outAllMp4Path);
                Directory.CreateDirectory(outAllMp4Path);
            }

            Log("文件夹名称前缀是否带原装avid的8位数字编号=" + isDirNameWithAvid);
            Log("MP4文件名称前缀是否带有原装文件夹排序编号=" + isMp4FileNameWithPageId);
            Log("是否根据文件大小自动确定休眠时间(是则设置的固定休眠时间不再使用)=" + isAutoCalcSleepTmes);

            int intSleepTimes = 1000;
            int.TryParse(sleepTimes, out intSleepTimes);
            Log("执行一个视频转换批处理后等待毫秒数=" + intSleepTimes);
            Log("是否只生成而不自动执行批处理(数据量小时 可以手动双击批处理执行)=" + isOnlyCreateBat);
            Log("是否运行完毕自动关闭程序界面(数据量过大时可以设置为1，方便结束时核对)=" + isAutoCloseApp);
            Log("是否先按需清理后再转换(先删除上次转换过的，再开始转换本次新加的)=" + isAutoClearBeforeConvert);
            Log("Server酱Key=" + serverChanKey);

            string desp = "";
            List<string> listDesp = new List<string>();

            Log("Download文件夹遍历开始");
            string[] directories_Convert = new string[] { };
            string[] directories = Directory.GetDirectories(bilibiliDownloadPath, "*", SearchOption.TopDirectoryOnly);
            if (directories != null && directories.Length > 0)
            {
                Log("共有" + directories.Length + "个文件夹。");

                //是否先按需删除
                if (isAutoClearBeforeConvert == "1")
                {

                    foreach (var dirName in directories)
                    {
                        Log(dirName + " 大文件夹删除开始");



                        Log("按需删除文件夹及文件");
                        ClearM4sDir(dirName);

                        //
                        string[] dirs = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
                        if (dirs != null && dirs.Length > 0)
                        {
                            //Nothing 可能还有新放入的待处理的文件夹，没有*.bat 或 *.txt文件，这个时候不能贸然删除大文件夹，需要检查
                            LogRed("可能还有新放入的待处理的文件夹，没有*.bat 或 *.txt文件，请核对");
                        }
                        else
                        {
                            Log("删除文件夹 " + dirName);
                            //删除文件夹及其子文件夹
                            Directory.Delete(dirName, true);
                        }



                        Log(dirName + " 大文件夹删除结束");
                    }
                    Log("Download文件夹遍历删除结束");


                }


                directories_Convert = Directory.GetDirectories(bilibiliDownloadPath, "*", SearchOption.TopDirectoryOnly);
                if (directories_Convert != null && directories_Convert.Length > 0)
                {
                    //遍历转换
                    foreach (var dirName in directories_Convert)
                    {
                        Log(dirName + " 大文件夹开始");
                        ConvertM4sToMp4(dirName, intSleepTimes, isOnlyCreateBat, ref listDesp);
                        Log(dirName + " 大文件夹结束");
                    }
                }
                Log("Download文件夹遍历结束");
            }
            else
            {
                LogRed("未发现符合要求的文件夹，请检查是否有文件夹？");

            }


            Log("-----------------------");

            if (!string.IsNullOrEmpty(serverChanKey))
            {

                desp = string.Format(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "转换为MP4批处理调用完毕。删除时文件夹个数："
                    + directories.Length + "转换时文件夹个数：" + directories_Convert.Length) + "   \r\n";

                if (listDesp.Count > 0)
                {
                    desp += "------------------";
                    for (int i = 0; i < listDesp.Count; i++)
                    {
                        desp += "(" + i.ToString() + "_[" + listDesp[i].ToString() + "])\r\n";
                    }
                }
                desp += "[------over------]";
                LogGreen(desp);
                SendMsgWithServerChan("Bilibili视频转换MP4提醒", desp);
            }


            if (isAutoCloseApp == "1")
            {
                //Nothing
            }
            else
            {
                LogGreen("按任意键可退出。。。");
                Console.ReadLine();
            }

        }

        /// <summary>
        /// 发送通知消息到手机
        /// </summary>
        /// <param name="text"></param>
        /// <param name="desp"></param>
        static void SendMsgWithServerChan(string text, string desp = "")
        {
            string url = string.Format("https://sctapi.ftqq.com/{0}.send?title={1}&desp={2}", serverChanKey, text, desp);

            try
            {
                HttpWebRequest httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Method = "GET";

                HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse; // 获取响应
                if (httpWebResponse != null)
                {
                    using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Log(content);
                            if (content.Contains("SUCCESS"))
                            {
                                LogGreen("通知发送成功。");
                            }
                            else
                            {
                                LogRed("未收到成功标识success，请注意检查。");
                            }
                        }
                    }

                    httpWebResponse.Close();
                }
            }
            catch (Exception ex)
            {
                LogRed("发送通知时出现异常：" + ex.Message);
            }


        }

        /// <summary>
        /// 按需删除文件夹及文件
        /// </summary>
        /// <param name="videoPath"></param>
        static void ClearM4sDir(string videoPath)
        {
            //查出来的都是要删除的
            string[] txtFiles = Directory.GetFiles(videoPath, "*.txt", SearchOption.TopDirectoryOnly);
            if (txtFiles != null && txtFiles.Length > 0)
            {

                string strBatName = "";
                string strDirName = "";
                foreach (var txtFile in txtFiles)
                {
                    strBatName = txtFile.Substring(0, txtFile.Length - 4) + ".bat";
                    strDirName = txtFile.Substring(0, txtFile.Length - 4);


                    if (Directory.Exists(strDirName))
                    {
                        Log("删除文件夹 " + strDirName);
                        Directory.Delete(strDirName, true);
                    }
                    else
                    {
                        LogRed("未找到文件夹 " + strDirName + " 可能已经被删除，请注意检查");
                    }
                    if (File.Exists(strBatName))
                    {
                        Log("删除批处理文件 " + strBatName);
                        File.Delete(strBatName);
                    }
                    else
                    {
                        LogRed("未找到批处理文件 " + strBatName + " 可能已经被删除，请注意检查");
                    }

                    Log("删除txt文本文件 " + txtFile);
                    File.Delete(txtFile);

                }



            }

        }

        /// <summary>
        /// 转换为MP4
        /// </summary>
        /// <param name="videoPath"></param>
        /// <param name="sleepTimes"></param>
        /// <param name="isOnlyCreateBat"></param>
        /// <param name="listDesp"></param>
        static void ConvertM4sToMp4(string videoPath, int sleepTimes, string isOnlyCreateBat, ref List<string> listDesp)
        {
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            Log(rootPath);
            string ffmpegPath = rootPath + @"ffmpeg\bin\";

            string[] directories = Directory.GetDirectories(videoPath, "*", SearchOption.TopDirectoryOnly);
            if (directories != null && directories.Length > 0)
            {
                foreach (var dirName in directories)
                {
                    LogGreen(dirName);

                    string jsonFile = dirName + "\\" + "entry.json";

                    if (File.Exists(jsonFile))
                    {
                        string jsonString = File.ReadAllText(jsonFile);
                        EntryModel entryModel = JsonConvert.DeserializeObject<EntryModel>(jsonString);
                        if (entryModel != null && entryModel.page_data != null)
                        {

                            LogGreen(entryModel.page_data.page + " 开始");
                            //保存的MP4文件夹名称
                            string title = entryModel.title;
                            title = ReplaceInvalidChar(title);

                            string outDirName = title;
                            if (isDirNameWithAvid == "1")
                            {
                                outDirName = entryModel.avid + "_" + title;
                            }
                            outDirName = outAllMp4Path + "\\" + outDirName;
                            if (!Directory.Exists(outDirName))
                            {
                                Directory.CreateDirectory(outDirName);
                            }

                            //添加到ServerChan集合，供发送时拼接用。
                            if (!listDesp.Contains(outDirName))
                            {
                                listDesp.Add(outDirName);
                            }


                            string mp4FileName = entryModel.page_data.page.ToString();// + ".mp4";
                            string subtitle = entryModel.page_data.download_subtitle;
                            if (!string.IsNullOrEmpty(entryModel.page_data.download_subtitle))
                            {
                                subtitle = ReplaceInvalidChar(subtitle);

                                mp4FileName = subtitle;
                                if (isMp4FileNameWithPageId == "1")
                                {
                                    mp4FileName = entryModel.page_data.page + "_" + subtitle;
                                }
                            }

                            string fullMp4File = "";
                            fullMp4File = outDirName + "\\"
                                            + ReplaceInvalidChar(mp4FileName)//把冒号等替换成下划线，否则文件名遇到冒号即终止，会发生没有.mp4结尾的情况
                                            + ".mp4";



                            if (!fullMp4File.EndsWith(".mp4"))
                            {
                                fullMp4File = fullMp4File + ".mp4";
                            }


                            string cmdText = "";


                            if (entryModel.type_tag == "lua.flv.bili2api.80" || entryModel.type_tag.Contains("flv"))
                            {


                                string[] blvFiles = Directory.GetFiles(dirName + @"\" + entryModel.type_tag, "*.blv", SearchOption.TopDirectoryOnly);
                                if (blvFiles != null && blvFiles.Length > 0)
                                {
                                    cmdText = "";
                                    for (int i = 0; i < blvFiles.Length; i++)
                                    {
                                        string item = blvFiles[i];
                                        string numText = item.Substring(item.LastIndexOf("\\") + 1, item.LastIndexOf(".") - item.LastIndexOf("\\") - 1);
                                        Console.WriteLine(numText);

                                        string blvFileName = dirName + @"\" + entryModel.type_tag + @"\" + numText + ".blv";
                                        string tempMp4FileName = mp4FileName;
                                        if (isMp4FileNameWithPageId == "1")
                                        {
                                            tempMp4FileName = tempMp4FileName.Insert(tempMp4FileName.IndexOf("_") + 1, i.ToString() + "_");
                                        }
                                        else
                                        {
                                            tempMp4FileName = tempMp4FileName.Insert(0, i.ToString() + "_");
                                        }

                                        fullMp4File = outDirName + "\\"
                                                       + ReplaceInvalidChar(tempMp4FileName)
                                                       + ".mp4";
                                        Log(fullMp4File);

                                        cmdText += "\r\n" + $"{ffmpegPath}ffmpeg -i {blvFileName} -vcodec copy -acodec copy \"{fullMp4File}\"";
                                        //cmdText += "\r\n" + $"(echo %date% %time%)>>{ videoPath + "\\"}{"c_"+entryModel.page_data.cid}.txt";
                                        cmdText += "\r\n" + $"(echo %date% %time%)>>{dirName}.txt";
                                    }

                                }

                            }
                            else
                            {

                                string videoFileName = dirName + @"\" + entryModel.type_tag + @"\video.m4s";
                                string audioFileName = dirName + @"\" + entryModel.type_tag + @"\audio.m4s";

                                //批处理中如果有中文，用双引号引起来就可以了。
                                cmdText = "\r\n" + $"{ffmpegPath}ffmpeg -i {videoFileName} -i {audioFileName} -c:v copy -strict experimental \"{fullMp4File}\"";
                                //cmdText += "\r\n" + $"(echo %date% %time%)>>{ videoPath + "\\"}{"c_" + entryModel.page_data.cid}.txt";
                                cmdText += "\r\n" + $"(echo %date% %time%)>>{dirName}.txt";

                                Log(fullMp4File);
                            }



                            Log(cmdText);

                            //string batFileName = videoPath + "\\" + "c_" + entryModel.page_data.cid + ".bat";
                            string batFileName = dirName + ".bat";
                            File.WriteAllText(batFileName, cmdText, Encoding.Default);//ANSI格式，批处理中才支持中文字符

                            if (isOnlyCreateBat == "1")
                            {
                                //Nothing
                            }
                            else
                            {
                                //string batTxtName = videoPath + "\\" + "c_" + entryModel.page_data.cid + ".txt";
                                string batTxtName = dirName + ".txt";
                                Log(batTxtName);
                                string batTxtString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 运行了批处理\r\n";
                                Log(batTxtString);
                                File.WriteAllText(batTxtName, batTxtString, Encoding.Default);//ANSI格式，批处理中才支持中文字符


                                Log("运行批处理");
                                Process.Start(batFileName);

                                if (isAutoCalcSleepTmes == "1")//智能模式，根据文件大小调整间隔休眠时间
                                {
                                    int fileSizeM = entryModel.total_bytes / (1024 * 1024);//得出单位是M兆
                                    Log("文件总大小=" + fileSizeM + "M");
                                    int sleepTimesByFileSize = 1000;

                                    if (fileSizeM < 100)
                                    {
                                        sleepTimesByFileSize = 5 * 1000;
                                    }
                                    else if (fileSizeM >= 100 && fileSizeM < 200)
                                    {
                                        sleepTimesByFileSize = 7 * 1000;
                                    }
                                    else if (fileSizeM >= 200 && fileSizeM < 500)
                                    {
                                        sleepTimesByFileSize = 10 * 1000;
                                    }
                                    else if (fileSizeM >= 500 && fileSizeM < 1000)
                                    {
                                        sleepTimesByFileSize = 12 * 1000;
                                    }
                                    else if (fileSizeM >= 1000 && fileSizeM < 2000)
                                    {
                                        sleepTimesByFileSize = 15 * 1000;
                                    }
                                    else if (fileSizeM >= 2000)
                                    {
                                        sleepTimesByFileSize = 20 * 1000;
                                    }

                                    LogGreen("开始自动休眠" + sleepTimesByFileSize + "毫秒");
                                    Thread.Sleep(sleepTimesByFileSize);
                                }
                                else//固定时间间隔模式
                                {
                                    //休眠配置文件中配置的时间 同时打开10个批处理是没有问题的，可以根据文件大小和转换速度 灵活的调整这个时间值
                                    LogGreen("开始休眠" + sleepTimes + "毫秒");
                                    Thread.Sleep(sleepTimes);
                                }



                            }
                            Log(entryModel.page_data.page + " 完成");

                        }


                    }


                }
            }



        }

        /// <summary>
        /// 将非法字符替换为下划线_符号
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string ReplaceInvalidChar(string title)
        {
            //含有非法字符
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            invalid += "\\";
            invalid += "/";
            invalid += ":";
            invalid += "*";
            invalid += "?";
            invalid += "\"";
            invalid += "<";
            invalid += ">";
            invalid += "|";
            invalid += " ";
            //invalid += "@";
            invalid += "#";
            invalid += "$";
            invalid += "%";
            invalid += "&";

            invalid += "？";
            invalid += "￥";
            invalid += "！";

            if (title.IndexOfAny(invalid.ToCharArray()) >= 0)
            {

                //替换为下划线_，只要发现路径或文件名中出现下划线，就有可能是含有非法字符造成的。算是一个标识。
                foreach (char c in invalid)
                {
                    title = title.Replace(c.ToString(), "_");
                }

            }

            return title;
        }

        /// <summary>
        /// 普通颜色提示
        /// </summary>
        /// <param name="logMsg"></param>
        static void Log(string logMsg)
        {
            Console.WriteLine(logMsg);
            logger.Info(logMsg);
        }

        /// <summary>
        /// 文字绿色提示
        /// </summary>
        /// <param name="logMsg"></param>
        static void LogGreen(string logMsg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(logMsg);
            logger.Info(logMsg);
            Console.ResetColor();
        }

        /// <summary>
        /// 文字红色提示
        /// </summary>
        /// <param name="logMsg"></param>
        static void LogRed(string logMsg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMsg);
            logger.Info(logMsg);
            Console.ResetColor();
        }

        /// <summary>
        /// 运行cmd命令
        /// 会显示命令窗口
        /// </summary>
        /// <param name="cmdExe">指定应用程序的完整路径</param>
        /// <param name="cmdStr">执行命令行参数</param>
        static bool RunCmd(string cmdExe, string cmdStr)
        {
            bool result = false;
            try
            {
                using (Process myPro = new Process())
                {
                    //指定启动进程是调用的应用程序和命令行参数
                    ProcessStartInfo psi = new ProcessStartInfo(cmdExe, cmdStr);
                    myPro.StartInfo = psi;
                    myPro.Start();
                    myPro.WaitForExit();
                    result = true;
                }
            }
            catch
            {

            }
            return result;
        }

        /// <summary>
        /// 运行cmd命令
        /// 不显示命令窗口
        /// </summary>
        /// <param name="cmdExe">指定应用程序的完整路径</param>
        /// <param name="cmdStr">执行命令行参数</param>
        static bool RunCmd2(string cmdExe, string cmdStr)
        {
            bool result = false;
            try
            {
                using (Process myPro = new Process())
                {
                    myPro.StartInfo.FileName = "cmd.exe";
                    myPro.StartInfo.UseShellExecute = false;
                    myPro.StartInfo.RedirectStandardInput = true;
                    myPro.StartInfo.RedirectStandardOutput = true;
                    myPro.StartInfo.RedirectStandardError = true;
                    myPro.StartInfo.CreateNoWindow = true;
                    myPro.Start();
                    //如果调用程序路径中有空格时，cmd命令执行失败，可以用双引号括起来 ，在这里两个引号表示一个引号（转义）
                    string str = string.Format(@"""{0}"" {1} {2}", cmdExe, cmdStr, "&exit");

                    myPro.StandardInput.WriteLine(str);
                    myPro.StandardInput.AutoFlush = true;
                    //获取输出信息
                    string strOuput = myPro.StandardOutput.ReadToEnd();

                    myPro.WaitForExit();

                    //等待程序执行完退出进程

                    myPro.Close();

                    Console.WriteLine(strOuput);

                    result = true;
                }
            }
            catch
            {

            }
            return result;
        }


    }
}
