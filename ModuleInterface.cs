namespace AGDev {
	public interface Taker<ItemType> {
		void Take(ItemType item);
		void None();
	}
	public interface Giver<ItemType, RequestType> {
		void Give(RequestType request, Taker<ItemType> taker);
	}
	public interface ImmediateGiver<ItemType, RequestType> {
		ItemType Give(RequestType request);
	}
	public interface Gate {
		bool isOpen { get; }
		void Open(bool doCloseImmediate);
		void Close();
	}
	public interface Trigger {
		void Pull(SimpleProcessListener simpleListener);
	}
	public interface SimpleProcessListener {
		void OnFinish(bool didSuccess);
	}
	public interface InterceptListener {
		void OnBeginWorking();
		void OnAllProcessDone();
	}
	public interface GeneralGiver<KeyType> {
		void Give<ElementType>(KeyType key, Taker<ElementType> taker);
	}
	public interface ConfigurationListener {
		void OnEnableConfigure<Type>(string name, Taker<Type> taker, Type initialValue);
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