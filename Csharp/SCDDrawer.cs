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

        // 保存已输出的母线位置信息
        private static IDictionary<int, IDictionary<int,int[]>> buses = new Dictionary<int,IDictionary<int,int[]>>();

        // 保存电压等级
        private static int High_volt;
        private static int Mid_volt;

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
            html.Save("index.html");
            Console.WriteLine("Draw done.");

            Console.ReadLine();
        }

        /// <summary>
        /// 通过解析得到的主变信息画出对应主变位置图，保存主变位置信息
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

        /// <summary>
        /// 通过解析得到的母线信息画出对应母线的位置图，保存母线位置信息
        /// </summary>
        private static void DrawBus()
        {
            var buses = SCDResolver.buses;
            int[] volts = buses.Keys.OfType<int>().ToArray();
            var num = volts.Count();
            if (num < 2)
                return;

            // 获取高，中压等级电压
            High_volt = volts[num - 1];
            Mid_volt  = volts[num - 2];

            // 高压侧是500kV及以上时，按照3/2接线方式处理
            if (High_volt >= 500)
            {
                // 画3/2接线
                draw_one_and_half();
            }
            // 高压侧是220kV及以下时，按照正常逻辑处理
            else
            {
                // 不存在关联关系的母线，直接画单独分段线段
                if(SCDResolver.buses_relation[High_volt].Count() == 0)
                {
                    // 每一段，单独画
                    foreach(var i in SCDResolver.buses[High_volt])
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 3/2接线的绘制方法
        /// </summary>
        private static void draw_one_and_half()
        {
            int x = 50, y = 200;

            foreach (int i in SCDResolver.buses[High_volt])
            {
                // 母线元素节点，及其属性设置
                var ele_attrs = new Dictionary<string, string>() {
                    { "id", High_volt.ToString()+ "kV_" +i.ToString()},
                    { "x1",x.ToString()},
                    { "y1",y.ToString()},
                    { "x2",(x+1200).ToString()},
                    { "y2",y.ToString()},
                };
                var ele = NewElement("use", ele_attrs);
                svg.AppendChild(ele);

                // 母线对应的文字，及其属性设置
                var text_attrs = new Dictionary<string, string>()
                {
                    { "dy", "0" } ,
                    { "stroke", "black" } ,
                    { "stroke-width", "0.5" } ,
                    { "x", (x-20).ToString() } ,
                    { "y", (y+5).ToString()}
                };
                var text = NewElement("text",text_attrs);
                text.InnerText = SCDResolver.c_index[i];
                svg.AppendChild(text);

                // 保存母线位置信息
                buses[High_volt] = new Dictionary<int,int[]>();
                buses[High_volt][i] = new int[] { x,y };

                // 调整 x, y
                y = y + 150;
            }
        }
    }
}
