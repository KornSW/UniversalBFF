using Logging.SmartStandards.EventKindManagement;
using System.ComponentModel;

namespace TemplateNamespace {

  [TypeConverter(typeof(EventKindEnumConverter))]
  internal enum EventKind {

    /// <summary> Kind: "The web-application has successfully completed its initialization." </summary>
    [LogMessageTemplate("The web-application has successfully completed its initialization.")]
    [LogMessageTemplate("Die Webanwendung hat ihre Initialisierung erfolgreich abgeschlossen.","de-de")]
    WebApplicationStarted = 60000,

  }

}
