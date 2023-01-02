using System.Collections.Generic;
using System.Xml.Serialization;


namespace FemDesign.StructureGrid
{
    /// <summary>
    /// Class to contain list in entities. For serialization purposes only.
    /// </summary>
    [System.Serializable]
    public partial class Storeys
    {
        [XmlElement("storey", Order = 1)]
        public List<Storey> Storey = new List<Storey>();
    }
}