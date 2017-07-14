using System;

using Verse;

namespace SurvivalistsAdditions {

  public interface IItemProcessor {

    float Progress { get; set; }

    ThingRequest InputRequest { get; }

    int SpaceLeftForItem { get; }

    bool Empty { get; }

    bool Finished { get; }

    bool TemperatureAcceptable { get; }

    int EstimatedTicksLeft { get; }

    Predicate<Thing> ItemValidator(Pawn pawn);

    int AddItem(Thing item);

    void Reset();

    Thing TakeOutProduct();
  }
}
