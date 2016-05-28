using System;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace PersistDotNet.Persist
{
    public sealed class YamlSerializer : TreeSerializer
    {
        public YamlSerializer(Type type, Type[] polymorphicTypes = null) : base(type, polymorphicTypes)
        {
        }

        protected override void WriteElement(Stream target, Element root)
        {
            YamlDocument doc = new YamlDocument(new YamlMappingNode());
            WriteElement((YamlMappingNode) doc.RootNode,root);

            using (var writer = new StreamWriter(target, new UTF8Encoding(false), 1024, true))
            {
                var stream = new YamlStream(doc);
                stream.Save(writer,false);
            }
        }
        protected override Element ParseElement(Stream source)
        {
            YamlDocument doc;

            using (var reader = new StreamReader(source, Encoding.UTF8, true, 1024, true))
            {
                var stream = new YamlStream();
                stream.Load(reader);

                doc = stream.Documents.Single();
            }

            var mainElement = new Element(string.Empty);

            ParseElement(doc.RootNode, mainElement);

            return mainElement;
        }

        internal static void ParseElement(YamlNode curNode, Element curElement)
        {
            if (curNode is YamlMappingNode)
            {
                foreach (var pair in ((YamlMappingNode)curNode).Children)
                {
                    if (pair.Value is YamlScalarNode)
                        curElement.Attributes.Add(new Attribute(( (YamlScalarNode) pair.Key ).Value, ( (YamlScalarNode) pair.Value ).Value));
                    else
                    {
                        var childElement = new Element(pair.Key.ToString());

                        curElement.Elements.Add(childElement);
                        ParseElement(pair.Value,childElement);
                    }
                }
            }
            else
            {
                curElement.IsArray = true;

                foreach (var node in ((YamlSequenceNode)curNode).Children)
                {
                    var childElement = new Element(string.Empty);

                    curElement.Elements.Add(childElement);
                    ParseElement(node, childElement);
                }
            }
        }
        internal static void WriteElement(YamlNode node, Element element)
        {
            if (node is YamlMappingNode)
                foreach (var attribute in element.Attributes)
                {
                    ((YamlMappingNode)node).Add(new YamlScalarNode(attribute.Name), new YamlScalarNode(attribute.Value));
                }
            else if (element.Attributes.Count > 0)
                throw new InvalidOperationException("arrays cannot contain attributes");

            foreach (var e in element.Elements)
            {
                YamlNode childNode;

                if (e.Elements.AllSame(_ => _.Name))
                {
                    childNode = new YamlSequenceNode();
                    WriteElement(childNode, e);
                }
                else if (e.Elements.CountSame(_ => _.Name) == 0)
                {
                    childNode = new YamlMappingNode();
                    WriteElement(childNode, e);
                }
                else
                    throw new InvalidOperationException("YamlSerializer/JsonSerializer cannot serialize an anonymous containers aka [Persist(\"\")]");

                if (node is YamlMappingNode)
                    ((YamlMappingNode)node).Children.Add(new YamlScalarNode(e.Name), childNode);
                else
                    ((YamlSequenceNode)node).Add(childNode);
            }
        }
    }
}