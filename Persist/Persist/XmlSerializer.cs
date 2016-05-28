using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace PersistDotNet.Persist
{
    public sealed class XmlSerializer : TreeSerializer
    {
        public XmlSerializer(Type type, Type[] polymorphicTypes = null) : base(type, polymorphicTypes)
        {
        }

        protected override void WriteElement(Stream target, Element root)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode mainNode = doc.CreateElement(root.Name);
            doc.AppendChild(mainNode);

            WriteElement(doc, mainNode, root);
            doc.Save(target);
        }
        protected override Element ParseElement(Stream source)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(source);

            Element mainElement = new Element(doc.DocumentElement.Name);

            ParseElement(doc.DocumentElement,mainElement);

            return mainElement;
        }

        internal static void ParseElement(XmlNode curNode, Element curElement)
        {
            curElement.Attributes.AddRange(curNode.Attributes.Cast<XmlAttribute>().Select(attribute => new Attribute(attribute.Name,attribute.Value)));

            foreach (XmlNode xmlNode in curNode.ChildNodes)
            {
                Element childElement = new Element(xmlNode.Name);
                curElement.Elements.Add(childElement);

                ParseElement(xmlNode, childElement);
            }

        }
        internal static void WriteElement(XmlDocument doc, XmlNode node, Element element)
        {
            foreach (var attribute in element.Attributes)
            {
                var attr = doc.CreateAttribute(attribute.Name);
                attr.Value = attribute.Value;
                node.Attributes.Append(attr);
            }

            foreach (var e in element.Elements)
            {
                var childNode = doc.CreateElement(e.Name);
                node.AppendChild(childNode);

                WriteElement(doc, childNode, e);
            }
        }
    }

}