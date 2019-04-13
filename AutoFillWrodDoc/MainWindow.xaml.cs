﻿using Aspose.Words;
using Aspose.Words.Replacing;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace AutoFillWrodDoc
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ComWordHelper wordHelper = new ComWordHelper();
            var p1 = "F:\\Test\\test1.docx";
            var p2 = "F:\\Test\\test2.docx";
            var p3 = "F:\\Test\\test3.docx";
            //wordHelper.InsertMerge(p1, p2, p3);
            AsposeWordHepler asposeWordHepler = new AsposeWordHepler();
            asposeWordHepler.MergeDocument(p1, p2);
            //asposeWordHepler.ReplaceString(p3, "Ω", "欧姆");
        }

        private void BtnDownLoadExcelModel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出Excel",
                FileName = "订单Excel模板",
                Filter = "xlsx 文件(*.xlsx)|*.xlsx|xls 文件(*.xls)|*.xls"
            };
            if (fileDialog.ShowDialog() == true)
            {
                DataTable dt = new DataTable();
                #region ==DataTable组装==
                dt.Columns.Add("规格型号");
                dt.Columns.Add("订单号");
                dt.Columns.Add("编号范围");
                dt.Columns.Add("环境温度");
                dt.Columns.Add("环境湿度");
                dt.Columns.Add("检验员");
                dt.Columns.Add("检验日期");
                dt.Columns.Add("审核人");
                dt.Columns.Add("审核日期");
                DataRow row2 = dt.NewRow();
                row2["规格型号"] = "从此行开始导入，温湿度填入不带单位,湿度为百分比";
                dt.Rows.Add(row2);
                #endregion
                var fileName = fileDialog.FileName;
                using (var helper = new NPOIExcelHepler(fileName))
                {
                    helper.DataTableToExcel(dt, "订单信息", true);
                }
            }
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpLoadWoExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtModelDirPath.Text))
                {
                    MessageBox.Show("请先选择检验报告模板所在文件夹");
                    return;
                }
                var workOrderInfos = new List<WorkOrderInfo>();
                //上传，读取数据
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "*.xls;*.xlsx| *.xls;*.xlsx"
                };
                if (ofd.ShowDialog() == true)
                {
                    using (var eh = new NPOIExcelHepler(ofd.FileName))
                    {
                        DataTable DtExcelBoms;
                        DtExcelBoms = eh.ExcelToDataTable(null);
                        if (DtExcelBoms == null)
                        {
                            MessageBox.Show("文件格式不正确，无法读取数据！");
                            eh.Dispose();
                        }
                        else if (DtExcelBoms.Rows.Count == 0)
                        {
                            MessageBox.Show("文件无有效数据！");
                            eh.Dispose();
                        }
                        else
                        {
                            var rowcount = DtExcelBoms.Rows.Count;
                            Dictionary<string, string> columnMap = new Dictionary<string, string>();
                            List<string> checkcolumnNames = new List<string> { "规格型号", "订单号", "编号范围", "环境温度", "环境湿度", "检验员", "检验日期", "审核人", "审核日期" };
                            List<string> columnNames = new List<string>();
                            foreach (DataColumn colum in DtExcelBoms.Columns)
                            {
                                columnNames.Add(colum.ColumnName);
                            }
                            var exlist = checkcolumnNames.Except(columnNames);
                            int excount = exlist.Count();
                            if (excount > 0)
                            {
                                MessageBox.Show("文件格式不正确，无法正确读取数据！");
                                return;
                            }

                            for (int i = 0; i < DtExcelBoms.Rows.Count; i++)
                            {
                                WorkOrderInfo workOrderInfo = new WorkOrderInfo
                                {
                                    Spec = Convert.ToString(DtExcelBoms.Rows[i]["规格型号"]).Trim(),
                                    WorkOrderNo = Convert.ToString(DtExcelBoms.Rows[i]["订单号"]).Trim(),
                                    NumberRange = Convert.ToString(DtExcelBoms.Rows[i]["编号范围"]).Trim(),
                                    Temperature = Convert.ToDouble(DtExcelBoms.Rows[i]["环境温度"]),
                                    Humidity = Convert.ToDouble(DtExcelBoms.Rows[i]["环境湿度"]),
                                    Tester = Convert.ToString(DtExcelBoms.Rows[i]["检验员"]).Trim(),
                                    TestDate = Convert.ToDateTime(DtExcelBoms.Rows[i]["检验日期"]),
                                    Auditor = Convert.ToString(DtExcelBoms.Rows[i]["审核人"]).Trim(),
                                    AuditDate = Convert.ToDateTime(DtExcelBoms.Rows[i]["审核日期"])
                                };
                                workOrderInfos.Add(workOrderInfo);
                            }
                        }
                    }
                }
                foreach (var woinfo in workOrderInfos)
                {
                    CreateReportWord(woinfo);
                }
                MessageBox.Show("报告文件生成结束。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CreateReportWord(WorkOrderInfo workOrderInfo)
        {
            //生成Word数据集
            FillTemplateWord fillTemplateWord = new FillTemplateWord();
            var result = fillTemplateWord.GetWordInfos(workOrderInfo);
            if (!result.Succeed)
            {
                var errmsg = "工单号：" + workOrderInfo.WorkOrderNo + ",错误信息：" + result.Message;
                MessageBox.Show(errmsg);
                return;
            }

            //找到模板文档
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(txtModelDirPath.Text);

            var specmodelfiles = directoryInfo.GetFiles().ToList();
            specmodelfiles = specmodelfiles.Where(x => x.Extension.Contains("doc") && x.Name.Contains(workOrderInfo.Spec + ")")).ToList();
            if (specmodelfiles == null || specmodelfiles.Count() == 0)
            {
                var errmsg = "工单号：" + workOrderInfo.WorkOrderNo + ",规格型号：" + workOrderInfo.Spec + "，错误信息：文件夹无此规格型号的模板。";
                MessageBox.Show(errmsg);
                return;
            }
            var wordmodelpath = specmodelfiles.FirstOrDefault().FullName;

            var wordlist = result.ChannelWordInfos;
            //根据Word数据集，生成对应名称的Word模板文档,并向每个文档填充对应数据
            AsposeWordHepler asposeWordHepler = new AsposeWordHepler();
            List<string> filnames = new List<string>();
            int i = 0;
            foreach (var word in wordlist)
            {
                string newfilename = "";
                if (i > 0)
                {
                    newfilename = word.WorkOrderNo + "出厂检验报告(" + word.Spec + ")-" + i.ToString() + ".docx";
                }
                else
                {
                    newfilename = word.WorkOrderNo + "出厂检验报告(" + word.Spec + ").docx";
                }

                var rpdirpath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Reports");
                if (!System.IO.Directory.Exists(rpdirpath))
                {
                    System.IO.Directory.CreateDirectory(rpdirpath);
                }
                var newfilefullname = System.IO.Path.Combine(rpdirpath, newfilename);

                asposeWordHepler.Copy(wordmodelpath, newfilefullname);
                ReplaceWord(newfilefullname, word);
                filnames.Add(newfilefullname);
                i++;
            }
            //第一个文档合并其他文档的数据
            if (filnames.Count() > 1)
            {
                var fristname = filnames.FirstOrDefault();
                filnames.Remove(fristname);
                foreach (var item in filnames)
                {
                    asposeWordHepler.MergeDocument(fristname, item);
                    System.IO.File.Delete(item);
                }
            }
        }

        private void BtnChooseWordModelDir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtModelDirPath.Text = dialog.SelectedPath;
            }
        }


        public void ReplaceWord(string filefullname, ChannelWordInfo wordInfo)
        {
            AsposeWordHepler asposeWordHepler = new AsposeWordHepler();
            Document doc = new Document(filefullname);
            FindReplaceOptions findReplaceOptions = new FindReplaceOptions();

            asposeWordHepler.ReplaceString(doc, "{wono}", wordInfo.WorkOrderNo);
            asposeWordHepler.ReplaceString(doc, "{no}", wordInfo.Number);
            asposeWordHepler.ReplaceString(doc, "{norg}", wordInfo.NumberRange);
            asposeWordHepler.ReplaceString(doc, "{qty}", wordInfo.Qty);
            asposeWordHepler.ReplaceString(doc, "{tp}", wordInfo.Temperature);
            asposeWordHepler.ReplaceString(doc, "{hd}", wordInfo.Humidity);
            asposeWordHepler.ReplaceString(doc, "{ter}", wordInfo.Tester);
            asposeWordHepler.ReplaceString(doc, "{tdate}", wordInfo.TestDate);
            asposeWordHepler.ReplaceString(doc, "{aer}", wordInfo.Auditor);
            asposeWordHepler.ReplaceString(doc, "{adate}", wordInfo.AuditDate);


            #region 1通道
            asposeWordHepler.ReplaceString(doc, "{c111u}", wordInfo.C1_U_1_0);
            asposeWordHepler.ReplaceString(doc, "{c112u}", wordInfo.C1_U_1_25);
            asposeWordHepler.ReplaceString(doc, "{c113u}", wordInfo.C1_U_1_50);
            asposeWordHepler.ReplaceString(doc, "{c114u}", wordInfo.C1_U_1_75);
            asposeWordHepler.ReplaceString(doc, "{c115u}", wordInfo.C1_U_1_100);

            asposeWordHepler.ReplaceString(doc, "{c111d}", wordInfo.C1_D_1_0);
            asposeWordHepler.ReplaceString(doc, "{c112d}", wordInfo.C1_D_1_25);
            asposeWordHepler.ReplaceString(doc, "{c113d}", wordInfo.C1_D_1_50);
            asposeWordHepler.ReplaceString(doc, "{c114d}", wordInfo.C1_D_1_75);
            asposeWordHepler.ReplaceString(doc, "{c115d}", wordInfo.C1_D_1_100);

            asposeWordHepler.ReplaceString(doc, "{c121u}", wordInfo.C1_U_2_0);
            asposeWordHepler.ReplaceString(doc, "{c122u}", wordInfo.C1_U_2_25);
            asposeWordHepler.ReplaceString(doc, "{c123u}", wordInfo.C1_U_2_50);
            asposeWordHepler.ReplaceString(doc, "{c124u}", wordInfo.C1_U_2_75);
            asposeWordHepler.ReplaceString(doc, "{c125u}", wordInfo.C1_U_2_100);

            asposeWordHepler.ReplaceString(doc, "{c121d}", wordInfo.C1_D_2_0);
            asposeWordHepler.ReplaceString(doc, "{c122d}", wordInfo.C1_D_2_25);
            asposeWordHepler.ReplaceString(doc, "{c123d}", wordInfo.C1_D_2_50);
            asposeWordHepler.ReplaceString(doc, "{c124d}", wordInfo.C1_D_2_75);
            asposeWordHepler.ReplaceString(doc, "{c125d}", wordInfo.C1_D_2_100);

            asposeWordHepler.ReplaceString(doc, "{b1}", wordInfo.C1_Bjqd);
            asposeWordHepler.ReplaceString(doc, "{cf1}", wordInfo.C1_Cfxwc);
            asposeWordHepler.ReplaceString(doc, "{hc1}", wordInfo.C1_Hc);
            asposeWordHepler.ReplaceString(doc, "{sq1}", wordInfo.C1_SQ);

            #endregion

            #region 2通道
            asposeWordHepler.ReplaceString(doc, "{c211u}", wordInfo.C2_U_1_0);
            asposeWordHepler.ReplaceString(doc, "{c212u}", wordInfo.C2_U_1_25);
            asposeWordHepler.ReplaceString(doc, "{c213u}", wordInfo.C2_U_1_50);
            asposeWordHepler.ReplaceString(doc, "{c214u}", wordInfo.C2_U_1_75);
            asposeWordHepler.ReplaceString(doc, "{c215u}", wordInfo.C2_U_1_100);
        
            asposeWordHepler.ReplaceString(doc, "{c211d}", wordInfo.C2_D_1_0);
            asposeWordHepler.ReplaceString(doc, "{c212d}", wordInfo.C2_D_1_25);
            asposeWordHepler.ReplaceString(doc, "{c213d}", wordInfo.C2_D_1_50);
            asposeWordHepler.ReplaceString(doc, "{c214d}", wordInfo.C2_D_1_75);
            asposeWordHepler.ReplaceString(doc, "{c215d}", wordInfo.C2_D_1_100);

            asposeWordHepler.ReplaceString(doc, "{c221u}", wordInfo.C2_U_2_0);
            asposeWordHepler.ReplaceString(doc, "{c222u}", wordInfo.C2_U_2_25);
            asposeWordHepler.ReplaceString(doc, "{c223u}", wordInfo.C2_U_2_50);
            asposeWordHepler.ReplaceString(doc, "{c224u}", wordInfo.C2_U_2_75);
            asposeWordHepler.ReplaceString(doc, "{c225u}", wordInfo.C2_U_2_100);

            asposeWordHepler.ReplaceString(doc, "{c221d}", wordInfo.C2_D_2_0);
            asposeWordHepler.ReplaceString(doc, "{c222d}", wordInfo.C2_D_2_25);
            asposeWordHepler.ReplaceString(doc, "{c223d}", wordInfo.C2_D_2_50);
            asposeWordHepler.ReplaceString(doc, "{c224d}", wordInfo.C2_D_2_75);
            asposeWordHepler.ReplaceString(doc, "{c225d}", wordInfo.C2_D_2_100);

            asposeWordHepler.ReplaceString(doc, "{b2}", wordInfo.C2_Bjqd);
            asposeWordHepler.ReplaceString(doc, "{cf2}", wordInfo.C2_Cfxwc);
            asposeWordHepler.ReplaceString(doc, "{hc2}", wordInfo.C2_Hc);
            asposeWordHepler.ReplaceString(doc, "{sq2}", wordInfo.C2_SQ);
            #endregion
            doc.Save(filefullname);
        }

    }
}
