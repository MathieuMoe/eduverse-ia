using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

/// <summary>Names of subjects, levels and topics from the resources, in the culture of the request.</summary>
public sealed class LocalizedCatalogNames(IStringLocalizer<SharedResource> l) : ICatalogNames
{
    public string Area(Area area) => l[$"Area_{area}"].Value;

    public string Level(Level level) => l[$"LevelShort_{level}"].Value;

    public string Topic(string topic) => QuizEndpoints.TopicName(topic, l);
}
