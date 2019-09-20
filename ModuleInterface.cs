namespace AGDev {
	public interface GateProcess {
		bool isOpen { get; }
		void Open(bool doCloseImmediate);
		void Close();
	}
	public interface SimpleTrigger {
		void Trigger(SimpleProcessListener simpleListener);
	}
	public interface SimpleProcessListener {
		void OnFinish(bool didSuccess);
	}
    public interface ModuleInterceptListener {
        void OnBeginWorking();
        void OnAllProcessDone();
    }
    public interface Collector<ItemType> {
		void Collect(ItemType item);
	}
	public interface AsyncCollector<ElementType> : Collector<ElementType> {
		void OnFinish();
	}
	public interface Picker<ElementType, KeyType> {
		void PickBestElement(KeyType key, AsyncCollector<ElementType> colletor);
	}
	public interface ImmediatePicker<ElementType, KeyType> {
		ElementType PickBestElement(KeyType key);
	}
	public interface GeneralPicker<KeyType> {
		void PickBestElement<ElementType>(KeyType key, AsyncCollector<ElementType> processor);
	}
	public interface ConfigurationListener {
		void OnEnableConfigure<Type>(string name, Collector<Type> collector, Type initialValue);
		void OnDisableConfigure(string name);
	}
	public interface Configurable {
		void ProvideConfiguration(ConfigurationListener listener);
	}
	public interface ObservedProcess {
		bool isBusy { get; }
		void AcceptObserver(ProcessObserver observer);
	}
	public interface ProcessObserver {
		void OnGetBusy();
		void OnGetIdle();
	}
}