using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SCDVisual
{
    class SCDDrawer
    {
        // 要使用的svg模板和处理的SVG节点
        private static XmlDocument html = new XmlDocument();
        private static XmlNode svg;

        // 保存已输出的主变位置信息
        private static IDictionary<int,int[]> trans = new Dictionary<int,int[]>();

        public static void Main(string[] args)
        {
            SCDResolver.init();
            try
            {
                html.Load("base.html");
                svg = html.SelectSingleNode("/html/body/svg");
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            DrawTransformer();

            Console.WriteLine("Draw done.");

            Console.ReadLine();
        }

        /// <summary>
        /// 通过解析得到的主变信息画出对应的图
        /// </summary>
        private static void DrawTransformer()
        {
            var trans = SCDResolver.transformers;
            // 主变的起始坐标
            int x = 0, y = 450;

            if (trans == null)
                return;
            // 遍历所有主变信息，生成各主变元素
            foreach (int trans_no in trans.Keys)
            {
                x = 300 + 400 * (trans_no-1);
                Dictionary<string, string> ele_attrs = new Dictionary<string, string>()
                {
                    { "id", (string)trans[trans_no] } ,
                    { "x", x.ToString() } ,
                    { "y", y.ToString() } ,
                    { "href", "#Trans" }
                };
                // 创建新节点
                XmlElement element = NewElement("use",ele_attrs);
                // 添加元素节点到svg节点后面
                svg.AppendChild(element);

                // 创建对应的文字信息
                Dictionary<string, string> text_attrs = new Dictionary<string, string>()
                {
                    { "dy", "0" } ,
                    { "stroke", "black" } ,
                    { "stroke-width", "0.5" } ,
                    { "x", (x-20).ToString() } ,
                    { "y", (y+25).ToString()}
                };
                // 添加文字节点大svg节点后面
                XmlElement text = NewElement("text",text_attrs);
                text.InnerText = (string)trans[trans_no];
                svg.AppendChild(text);

                // 保存此主变位置信息
                trans[trans_no] = new int[] {x,y };
            }
            // 保存，输出到新的文件
            html.Save("index.html");

        }

        /// <summary>
        /// 根据TagName新建一个XmlElement,并为其设置各项属性值信息
        /// </summary>
        /// <param name="name">XmlElement的TagName，字符串类型</param>
        /// <param name="attrs">XmlElement的属性集，字典类型</param>
        /// <returns>返回创建好的XmlElement元素</returns>
        private static XmlElement NewElement(string name, Dictionary<string,string> attrs)
        {
            // 创建子节点元素
            XmlElement element = html.CreateElement(name);
            // 设置元素的属性
            foreach (var attr in attrs)
            {
                element.SetAttribute(attr.Key,attr.Value);
            }
            return element;

        }
    }
}
