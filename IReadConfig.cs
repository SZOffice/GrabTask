using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;
using System.Xml;
using System.Collections;

namespace GrabTask
{
    public class IReadConfig : IConfigurationSectionHandler
    {
        object IConfigurationSectionHandler.Create(object parent, object configContext, XmlNode section)
        {

            const string nodeName = "add";
            const string nodeKey = "key";
            Hashtable myConfigObject = new Hashtable();
            Hashtable myAttribs = new Hashtable();
            foreach (XmlAttribute attrib in section.Attributes)
            {
                if (XmlNodeType.Attribute == attrib.NodeType)
                    myAttribs.Add(attrib.Name, attrib.Value);
            }

            myConfigObject.Add(section.Name, myAttribs);

            foreach (XmlNode child in section.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType && child.Name == nodeName)
                {
                    Hashtable myChildAttribs = new Hashtable();

                    foreach (XmlAttribute childAttrib in child.Attributes)
                    {
                        if (XmlNodeType.Attribute == childAttrib.NodeType && childAttrib.Name == nodeKey)
                            myConfigObject.Add(childAttrib.Value, child.InnerText);
                    }
                }
            }

            return myConfigObject;
        }
    }
}
