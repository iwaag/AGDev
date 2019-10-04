using System;
using System.Collections;
using System.Collections.Generic;
namespace AGDev.StdUtil {
	public class Utilities {
		public static bool CompareNullableString(string str, string str2) {
			return string.IsNullOrEmpty(str) && string.IsNullOrEmpty(str2) ? true : str == str2;
		}
		public static int CountEnumerableElements<ElemType>(IEnumerable<ElemType> enumerable) {
			int count = 0;
			foreach (var elem in enumerable) {
				count++;
			}
			return count;
		}
		public static bool AddIfNotDuplicated<ElemType>(List<ElemType> list, ElemType elem) {
			if (!list.Contains(elem)) {
				list.Add(elem);
				return true;
			}
			return false;
		}
	}
	public class ObservedProcessHelper : ObservedProcess {
		public List<ProcessObserver> observers = new List<ProcessObserver>();
		float count = 0;

		bool ObservedProcess.isBusy => count > 0;

		public void CountUp() {
			count++;
			if (count == 1) {
				foreach (var observer in observers) {
					observer.OnGetBusy();
				}
			}
		}
		public void CountDown() {
			count--;
			if (count == 0) {
				foreach (var observer in observers) {
					observer.OnGetIdle();
				}
			}
		}
		void ObservedProcess.AcceptObserver(ProcessObserver observer) {
			observers.Add(observer);
		}
	}
	public class ConvertingEnumarable<EnumeratedType, SourceType> : IEnumerable<EnumeratedType> {
		public IEnumerable<SourceType> sourceEnumerable;
		public Func<SourceType, EnumeratedType> convertFunc;
		IEnumerator<EnumeratedType> IEnumerable<EnumeratedType>.GetEnumerator() {
			return new GetComponentEnumarator { gObjEnumerator = sourceEnumerable.GetEnumerator(), ConvertFunc = convertFunc };
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new GetComponentEnumarator { gObjEnumerator = sourceEnumerable.GetEnumerator() };
		}
		public class GetComponentEnumarator : IEnumerator<EnumeratedType> {
			public IEnumerator<SourceType> gObjEnumerator;
			public Func<SourceType, EnumeratedType> ConvertFunc;
			EnumeratedType IEnumerator<EnumeratedType>.Current {
				get {
					return ConvertFunc(gObjEnumerator.Current);
				}
			}

			object IEnumerator.Current { get { return ConvertFunc(gObjEnumerator.Current); } }

			void IDisposable.Dispose() {
				gObjEnumerator.Dispose();
			}

			bool IEnumerator.MoveNext() {
				return gObjEnumerator.MoveNext();
			}

			void IEnumerator.Reset() {
				gObjEnumerator.Reset();
			}
		}
	}
	public class LoggingSimpleProcessListener : SimpleProcessListener {
		public SimpleProcessListener clientListener;
		public bool didSuccess = false;

		void SimpleProcessListener.OnFinish(bool _didSuccess) {
			didSuccess = _didSuccess;
			clientListener.OnFinish(didSuccess);
		}
	}
	public class StubSimpleProcessListener : SimpleProcessListener {
		void SimpleProcessListener.OnFinish(bool didSuccess) {
		}
	}
	public interface Counter {
		void CountUp();
	}
	public class EventCounter : Counter {
		int copySuccessCount = 0;
		public int copySuccessCountGoal = 1;
		public System.Action actionOnGoal;
		void Counter.CountUp() {
			copySuccessCount++;
			if (copySuccessCountGoal == copySuccessCount) {
				actionOnGoal();
			}
		}
	}
	#region collector
	public class EasyAsyncCollector<Type> : AsyncCollector<Type> {
		public Type collected;
		public bool didFinish = false;
		void Collector<Type>.Collect(Type item) {
			collected = item;
		}
		void AsyncCollector<Type>.OnFinish() {
			didFinish = true;
		}
	}
	public class LoggingCollector<Type> : Collector<Type> {
		public Collector<Type> client;
		public bool didCollect = false;
		public void Collect(Type newElement) {
			didCollect = true;
			client.Collect(newElement);
		}
	}
	public class ClusterCollector<Type> : Collector<Type> {
		public IEnumerable<Collector<Type>> collectors;
		public void Collect(Type newElement) {
			foreach (var collector in collectors) {
				collector.Collect(newElement);
			}
		}
	}
	public class ClusterAsyncCollector<ResultType> : AsyncCollector<ResultType> {
		public IEnumerable<AsyncCollector<ResultType>> collectors;
		void Collector<ResultType>.Collect(ResultType newElement) {
			foreach (var collector in collectors) {
				collector.Collect(newElement);
			}
		}

		void AsyncCollector<ResultType>.OnFinish() {
			foreach (var collector in collectors) {
				collector.OnFinish();
			}
		}
	}
	public class ToBaseAsyncCollector<SubType, BaseType> : AsyncCollector<SubType> {
		public AsyncCollector<BaseType> baseCollector;
		void Collector<SubType>.Collect(SubType newElement) {
			baseCollector.Collect((BaseType)(object)newElement);
		}
		void AsyncCollector<SubType>.OnFinish() {
			baseCollector.OnFinish();
		}
	}
	public class CollectorBridge<ElementType> : AsyncCollector<ElementType> {
		public Collector<ElementType> collector;
		void Collector<ElementType>.Collect(ElementType newElement) {
			collector.Collect(newElement);
		}
		void AsyncCollector<ElementType>.OnFinish() { }
	}
	public class CollectingBuildDirector<ResultType> {
		public AsyncCollector<ResultType> productCollector;
		public ResultType productToBuild;
		List<ResultHolder> buildResults = new List<ResultHolder>();
		public AsyncCollector<IntegrantType> NewIntegrantCollector<IntegrantType>(Action<ResultType, IntegrantType, SimpleProcessListener> buidingAction) {
			var resultHolder = new ResultHolder();
			buildResults.Add(resultHolder);
			return new CollectingBuilder<IntegrantType> { buidingAction = buidingAction, parent = this, resultHolder = resultHolder };
		}
		class ResultHolder {
			public bool didDetermin = false;
			public bool didSuccess = false;
		}
		public void CheckResult() {
			if (productCollector == null)
				return;
			foreach (var buildResult in buildResults) {
				if (buildResult.didDetermin) {
					if (!buildResult.didSuccess) {
						//build failed
						productCollector.OnFinish();
						productCollector = null;
						return;
					}
				}
				else {
					//not completed
					return;
				}
			}
			//completed
			productCollector.Collect(productToBuild);
			productCollector.OnFinish();
		}
		class CollectingBuilder<IntegrantType> : AsyncCollector<IntegrantType>, SimpleProcessListener {
			public Action<ResultType, IntegrantType, SimpleProcessListener> buidingAction;
			public ResultHolder resultHolder;
			public CollectingBuildDirector<ResultType> parent;
			void Collector<IntegrantType>.Collect(IntegrantType integrant) {
				buidingAction(parent.productToBuild, integrant, this);
			}

			void AsyncCollector<IntegrantType>.OnFinish() {
			}

			void SimpleProcessListener.OnFinish(bool didSuccess) {
				resultHolder.didDetermin = true;
				resultHolder.didSuccess = didSuccess;
				parent.CheckResult();
			}
		}
	}
	#endregion
	#region picker
	public class ClusterImmediatePicker<ElementType, KeyType> : ImmediatePicker<ElementType, KeyType> {
		public IEnumerable<ImmediatePicker<ElementType, KeyType>> pickers;
		ElementType ImmediatePicker<ElementType, KeyType>.PickBestElement(KeyType key) {
			foreach (var picker in pickers) {
				var elem = picker.PickBestElement(key);
				if (elem != null) {
					return elem;
				}
			}
			return default(ElementType);
		}
	}
	class DictionaryPicker<ElementType, KeyType> : ImmediatePicker<ElementType, KeyType> {
		public Dictionary<KeyType, ElementType> dict = new Dictionary<KeyType, ElementType>();
		ElementType ImmediatePicker<ElementType, KeyType>.PickBestElement(KeyType key) {
			dict.TryGetValue(key, out var result);
			return result;
		}
	}
	public class PickerLineup<ElementType, KeyType> : Picker<ElementType, KeyType> {
		public IEnumerable<Picker<ElementType, KeyType>> subPickers;
		void Picker<ElementType, KeyType>.PickBestElement(KeyType key, AsyncCollector<ElementType> colletor) {
			var enumerator = subPickers.GetEnumerator();
			if (enumerator.MoveNext())
				enumerator.Current.PickBestElement(key, new PrvtCollector { enumerator = enumerator, clientColletor = colletor, key = key });
		}
		public class PrvtCollector : AsyncCollector<ElementType> {
			public IEnumerator<Picker<ElementType, KeyType>> enumerator;
			public AsyncCollector<ElementType> clientColletor;
			public KeyType key;
			bool didPick = false;
			void Collector<ElementType>.Collect(ElementType item) {
				clientColletor.Collect(item);
				didPick = true;
			}

			void AsyncCollector<ElementType>.OnFinish() {
				if (!didPick && enumerator.MoveNext()) {
					enumerator.Current.PickBestElement(key, this);
					return;
				}
				clientColletor.OnFinish();
			}
		}
	}
	public class DefaultValuePicker<ElementType, KeyType> : Picker<ElementType, KeyType> {
		public ElementType defaultValue;
		void Picker<ElementType, KeyType>.PickBestElement(KeyType key, AsyncCollector<ElementType> colletor) {
			colletor.Collect(defaultValue);
			colletor.OnFinish();
		}
	}
	public class StubAsyncCollector<ItemType> : AsyncCollector<ItemType> {
		void Collector<ItemType>.Collect(ItemType item) { }
		void AsyncCollector<ItemType>.OnFinish() { }
	}
	#endregion
	#region behavior trigger
	public class ClusterBListener : BehaviorListener {
		public List<BehaviorListener> bListeners = new List<BehaviorListener>();
		void BehaviorListener.OnFinish() {
			foreach (var bListener in bListeners) {
				bListener.OnFinish();
			}
		}
	}
	class StubBehaviorListener : BehaviorListener {
		void BehaviorListener.OnFinish() { }
	}
	public class SerialClusterBTrigger : BehaviorTrigger {
		public IEnumerable<BehaviorTrigger> serialTriggers;
		void BehaviorTrigger.BeginBehavior(BehaviorListener behaviorListener) {
			var enumerator = serialTriggers.GetEnumerator();
			if (enumerator.MoveNext())
				enumerator.Current.BeginBehavior(new PrvtListener { clientListener = behaviorListener, enumerator = enumerator });
		}
		public class PrvtListener : BehaviorListener {
			public BehaviorListener clientListener;
			public IEnumerator<BehaviorTrigger> enumerator;
			void BehaviorListener.OnFinish() {
				if (enumerator.MoveNext())
					enumerator.Current.BeginBehavior(this);
				else
					clientListener.OnFinish();
			}
		}
	}
	#endregion

}
