using System;

namespace ProximityDemo_Universal
{
	public class EventArgs<T>:EventArgs
	{
		public EventArgs(T payload)
		{
			Payload = payload;
		}

		public T Payload { get; private set; }
	}
}
