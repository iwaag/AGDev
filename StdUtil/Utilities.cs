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
		public static ElemType GetElementAt<ElemType>(IEnumerable<ElemType> enumerable, int index){
			int currentIndex = 0;
			foreach (var element in enumerable) {
				if (index == currentIndex)
					return element;
				currentIndex++;
			}
			return default;
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
		public Func<SourceType, EnumeratedType> convertFunc = (source)=>(EnumeratedType)(object)source ;
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
	public class EasyTaker<Type> : Taker<Type> {
		public Type collected;
		public bool didFinish = false;
		void Taker<Type>.Take(Type item) {
			collected = item;
			didFinish = true;
		}
		void Taker<Type>.None() {
			didFinish = true;
		}
	}
	public class LoggingTaker<Type> : Taker<Type> {
		public Taker<Type> client;
		public bool didCollect = false;
		public void Take(Type newElement) {
			didCollect = true;
			client.Take(newElement);
		}

		void Taker<Type>.None() {
			client.None();
			didCollect = false;
		}

		void Taker<Type>.Take(Type item) {
			client.Take(item);
			didCollect = true;
		}
	}
	public class ClusterTaker<ResultType> : Taker<ResultType> {
		public IEnumerable<Taker<ResultType>> collectors;
		void Taker<ResultType>.Take(ResultType newElement) {
			foreach (var collector in collectors) {
				collector.Take(newElement);
			}
		}

		void Taker<ResultType>.None() {
			foreach (var collector in collectors) {
				collector.None();
			}
		}
	}
	public class ToBaseTaker<SubType, BaseType> : Taker<SubType> {
		public Taker<BaseType> baseTaker;
		void Taker<SubType>.Take(SubType newElement) {
			baseTaker.Take((BaseType)(object)newElement);
		}
		void Taker<SubType>.None() {
			baseTaker.None();
		}
	}
	#endregion
	#region Giver
	public class ClusterImmediateGiver<ElementType, KeyType> : ImmediateGiver<ElementType, KeyType> {
		public IEnumerable<ImmediateGiver<ElementType, KeyType>> pickers;
		ElementType ImmediateGiver<ElementType, KeyType>.PickBestElement(KeyType key) {
			foreach (var picker in pickers) {
				var elem = picker.PickBestElement(key);
				if (elem != null) {
					return elem;
				}
			}
			return default(ElementType);
		}
	}
	class DictionaryGiver<ElementType, KeyType> : ImmediateGiver<ElementType, KeyType> {
		public Dictionary<KeyType, ElementType> dict = new Dictionary<KeyType, ElementType>();
		ElementType ImmediateGiver<ElementType, KeyType>.PickBestElement(KeyType key) {
			dict.TryGetValue(key, out var result);
			return result;
		}
	}
	public class GiverLineup<ElementType, KeyType> : Giver<ElementType, KeyType> {
		public IEnumerable<Giver<ElementType, KeyType>> subGivers;
		void Giver<ElementType, KeyType>.Give(KeyType key, Taker<ElementType> colletor) {
			var enumerator = subGivers.GetEnumerator();
			if (enumerator.MoveNext())
				enumerator.Current.Give(key, new PrvtTaker { enumerator = enumerator, clientColletor = colletor, key = key });
		}
		public class PrvtTaker : Taker<ElementType> {
			public IEnumerator<Giver<ElementType, KeyType>> enumerator;
			public Taker<ElementType> clientColletor;
			public KeyType key;
			void Taker<ElementType>.Take(ElementType item) {
				clientColletor.Take(item);
			}

			void Taker<ElementType>.None() {
				if (enumerator.MoveNext()) {
					enumerator.Current.Give(key, this);
					return;
				}
				else {
					clientColletor.None();
				}
			}
		}
	}
	public class DefaultValueGiver<ElementType, KeyType> : Giver<ElementType, KeyType> {
		public ElementType defaultValue;
		void Giver<ElementType, KeyType>.Give(KeyType key, Taker<ElementType> colletor) {
			colletor.Take(defaultValue);
		}
	}
	public class StubGiver<ElementType, KeyType> : Giver<ElementType, KeyType> {
		void Giver<ElementType, KeyType>.Give(KeyType request, Taker<ElementType> taker) {
			taker.None();
		}
	}
	public class StubImmediateGiver<ElementType, KeyType> : ImmediateGiver<ElementType, KeyType> {
		ElementType ImmediateGiver<ElementType, KeyType>.PickBestElement(KeyType request) {
			return default;
		}
	}
	public class StubGeneralGiver<RequestType> : GeneralGiver<RequestType> {
		void GeneralGiver<RequestType>.Give<ItemType>(RequestType request, Taker<ItemType> taker) {
			taker.None();
		}
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
