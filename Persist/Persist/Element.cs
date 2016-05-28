using System.Collections.Generic;
using System.Linq;

namespace PersistDotNet.Persist
{
    public class Element
    {

        public virtual string Name { get; }
        public virtual long Id { get; set; }
        public virtual bool IsArray { get; set; }

        public virtual List<Attribute> Attributes { get; }
        public virtual List<Element> Elements { get; }
       

        public Element(string name)
        {
            Name = name;
            Attributes = new List<Attribute>();
            Elements = new List<Element>();
        }

        public Element(Element copyElement)
        {
            Name = copyElement.Name;
            Attributes = copyElement.Attributes.ToList();
            Elements = copyElement.Elements.Select(element => new Element(element)).ToList();
        }

        public override string ToString()
        {
            return Name;
        }

    }
}