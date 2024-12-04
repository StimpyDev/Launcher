using System.Xml.Serialization;
using System.Collections.Generic;

namespace Launcher.Models;

public sealed class ClientFolder
{
    [XmlAttribute("name")]
    public required string Name { get; set; }

    [XmlElement("File")]
    public List<ClientFile> Files { get; set; } = [];

    [XmlElement("Folder")]
    public List<ClientFolder> Folders { get; set; } = [];
}