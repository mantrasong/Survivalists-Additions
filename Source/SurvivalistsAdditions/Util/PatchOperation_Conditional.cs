using Verse;
using System.Xml;

namespace SurvivalistsAdditions {

  public class PatchOperation_Conditional : PatchOperation {

    protected PatchOperation testFor;
    protected PatchOperation operationTrue;
    protected PatchOperation operationFalse;

    protected override bool ApplyWorker(XmlDocument xml) {
      if (testFor.Apply(xml)) {
        return operationTrue.Apply(xml);
      }
      return operationFalse.Apply(xml);
    }
  }
}
