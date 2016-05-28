using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace PersistDotNet.Persist
{
    public sealed class JsonSerializer : TreeSerializer
    {

        public JsonSerializer(Type type, Type[] polymorphicTypes = null) : base(type, polymorphicTypes)
        {
        }

        protected override void WriteElement(Stream target, Element root)
        {
            YamlDocument doc = new YamlDocument(new YamlMappingNode());
            YamlSerializer.WriteElement((YamlMappingNode)doc.RootNode, root);

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream,new UTF8Encoding(false),1024,true))
                    new YamlStream(doc).Save(writer);

                stream.Seek(0, SeekOrigin.Begin);

                object yamlDynamicObj;
                using (var reader = new StreamReader(stream,Encoding.UTF8,true,1024,true))
                    yamlDynamicObj = new Deserializer().Deserialize(reader);

                using (var jsonWriter = new StreamWriter(target, new UTF8Encoding(false), 1024, true))
                    new Newtonsoft.Json.JsonSerializer {Formatting = Formatting.Indented}.Serialize(jsonWriter, yamlDynamicObj);
            }

        }
        protected override Element ParseElement(Stream source)
        {
            YamlDocument doc;
            using (var reader = new StreamReader(source, Encoding.UTF8, true, 1024, true))
            {
                var yamlReader = new YamlStream();
                yamlReader.Load(reader);
                doc = yamlReader.Documents.Single();
            }

            var mainElement = new Element(string.Empty);
            YamlSerializer.ParseElement(doc.RootNode, mainElement);

            return mainElement;
        }
    }
}