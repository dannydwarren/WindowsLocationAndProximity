using System;

namespace LocationDemo_Win8.FeatureWrappers
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
