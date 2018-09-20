using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace SCDVisual
{
    class SCDReader
    {
        public static void test(string[] args)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            const string file = "BZB.scd";
            XmlReader reader = XmlReader.Create(file);

            List<ArrayList> table = new List<ArrayList>();

            int tmp = 1;
            int count = 0;
            bool in_ied = false;

            while (reader.Read())
            {

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "IED")
                {
                    // 将IED信息添加到表格
                    AddItem(table, reader.GetAttribute("name"), reader.GetAttribute("desc"), 1, tmp, tmp);

                    in_ied = true;
                    // 统计IED数量
                    count++;

                    continue;
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "IED")
                {
                    in_ied = false;
                    continue;
                }

                if (in_ied  && reader.NodeType==XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "AccessPoint":
                            // 将AccessPoint信息添加进表格
                            AddItem(table, "AccessPoint", reader.GetAttribute("name"), 2, tmp, tmp);
                            break;
                        case "LDevice":
                            AddItem(table, reader.GetAttribute("inst"), reader.GetAttribute("desc"), 3, tmp, tmp);
                            break;
                        case "LN0":
                            string ln0_class = reader.GetAttribute("lnClass");
                            InputsAttr inputs = new InputsAttr();

                            if (reader.IsEmptyElement)
                            {
                                inputs.ExtRefs = null;
                                AddItem(table, ln0_class, inputs, 4, tmp, tmp);
                                break;
                            }
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "LN0")
                                    break;
                                if (reader.Name == "ExtRef")
                                {
                                    string ied = reader.GetAttribute("iedName");
                                    string ldInst = reader.GetAttribute("ldInst");
                                    string lnClass = reader.GetAttribute("lnClass");
                                    string lnInst = reader.GetAttribute("lnInst");
                                    inputs.ExtRefs.Add(new string[] { ied, ldInst, lnClass, lnInst });
                                }
                            }
                            AddItem(table, ln0_class, inputs, 4, tmp, tmp);
                            break;
                        case "LN":
                            AddItem(table, reader.GetAttribute("lnClass") + "_" + reader.GetAttribute("lnInst"), reader.GetAttribute("desc"), 4, tmp, tmp);
                            break;
                        default:
                            break;
                    }
                }

                if (reader.Name == "DataTypeTemplates")
                    break;               
            }

            stopwatch.Stop();
            Console.WriteLine("Total IED:"+count);
            Console.WriteLine("Total time span:"+stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 将数据项添加进表格
        /// </summary>
        /// <param name="tb">表格对象</param>
        /// <param name="identifier">标识符</param>
        /// <param name="refer">表格项的引用对象</param>
        /// <param name="level">层级</param>
        /// <param name="lt">左值</param>
        /// <param name="rt">右值</param>
        private static void AddItem(IList tb,string identifier,object refer,int level,int lt,int rt)
        {
            ArrayList arr = new ArrayList();
            // 添加标签名
            arr.Add(identifier);
            // 添加引用
            arr.Add(refer);
            // 添加level
            arr.Add(level);
            // 添加左值lt
            arr.Add(lt);
            // 添加右值rt
            arr.Add(rt);
            // 添加到表格
            tb.Add(arr);
        }
    }

    class InputsAttr
    {
        public List<string[]> ExtRefs = new List<string[]>();
    }
}
