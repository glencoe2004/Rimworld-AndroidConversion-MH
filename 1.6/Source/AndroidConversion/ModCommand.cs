using Verse;

namespace AndroidConversion;

public abstract class ModCommand : IExposable
{
	public AndroidModDef def;

	public bool removing;

	public bool isActive;

	public void ExposeData()
	{
		Scribe_Values.Look(ref removing, "removing", defaultValue: false);
		Scribe_Values.Look(ref isActive, "isActive", defaultValue: false);
		Scribe_Defs.Look(ref def, "targetDef");
	}

	public abstract bool Active(Pawn customTarget);

	public abstract void Apply(Pawn customTarget);

	public abstract void Remove(Pawn customTarget);

	public abstract string GetExplanation();
}
