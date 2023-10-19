namespace BunnyLibs.ParentInterfaces
{
	//	This is a parent interface with no function on its own.
	public interface IRefreshPerLevel
	{
		/// <summary>
		/// Set to TRUE in order to apply Refresh() regardless of whether any unlocks are active.
		/// </summary>
		bool BypassUnlockChecks { get; }

		/// <summary><code>
		/// Disaster, Mutator:                                   Applies if active
		/// </code></summary>
		void Refresh();

		/// <summary><code>
		/// Disaster, Mutator:                                   Applies to all agents if active
		/// Status Effect, Trait:                                Applies to agents who have the unlock
		/// Note that this will NOT run with BypassUnlockChecks, only Refresh() will.
		/// </code></summary>
		void Refresh(Agent agent);

		/// <summary>
		/// Must be true for any of the Refresh methods to run.
		/// </summary>
		bool RunThisLevel();
	}
}