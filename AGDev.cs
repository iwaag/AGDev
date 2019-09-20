namespace AGDev {
	public interface LanguageToBehavior<LanguageType> {
		BehaviorTrigger InterpretLanguageAsBehavior(LanguageType language);
	}
	public interface BehaviorTrigger {
		void BeginBehavior(BehaviorListener behaviorListener);
	}
	public interface BehaviorListener {
		void OnFinish();
	}
	public interface BehaviorController {
		void RequestStop();
		void RequestPlay();
	}
	public interface NaturalLanguageToBehavior : LanguageToBehavior<string> {}
	
}