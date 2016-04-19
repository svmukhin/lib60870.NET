using System;

namespace lib60870
{

	public class ParameterNormalizedValue : InformationObject
	{
		private ScaledValue scaledValue;

		public float NormalizedValue {
			get {
				float nv = (float) (scaledValue.Value) / 32767f;

				return nv;
			}

			set {
				//TODO check value range
				scaledValue.Value = (int)(value * 32767f); 
			}
		}

		private byte qpm;

		public float QPM {
			get {
				return qpm;
			}
		}

		public ParameterNormalizedValue (int objectAddress, float normalizedValue, byte qpm) :
			base (objectAddress)
		{
			scaledValue = new ScaledValue ((int)(normalizedValue * 32767f));

			this.NormalizedValue = normalizedValue;

			this.qpm = qpm;
		}

		public ParameterNormalizedValue (ConnectionParameters parameters, byte[] msg, int startIndex) :
			base(parameters, msg, startIndex)
		{
			startIndex += parameters.SizeOfIOA; /* skip IOA */

			scaledValue = new ScaledValue (msg, startIndex);
			startIndex += 2;

			/* parse QDS (quality) */
			qpm = msg [startIndex++];
		}

		public override void Encode(Frame frame, ConnectionParameters parameters) {
			base.Encode(frame, parameters);

			frame.AppendBytes (scaledValue.GetEncodedValue ());

			frame.SetNextByte (qpm);
		}
	}

	public class ParameterScaledValue : InformationObject
	{
		private ScaledValue scaledValue;

		public ScaledValue ScaledValue {
			get {
				return scaledValue;
			}
			set {
				scaledValue = value;
			}
		}

		private byte qpm;

		public float QPM {
			get {
				return qpm;
			}
		}

		public ParameterScaledValue (int objectAddress, ScaledValue value, byte qpm) :
			base (objectAddress)
		{
			scaledValue = value;

			this.qpm = qpm;
		}

		public ParameterScaledValue (ConnectionParameters parameters, byte[] msg, int startIndex) :
			base(parameters, msg, startIndex)
		{
			startIndex += parameters.SizeOfIOA; /* skip IOA */

			scaledValue = new ScaledValue (msg, startIndex);
			startIndex += 2;

			/* parse QDS (quality) */
			qpm = msg [startIndex++];
		}

		public override void Encode(Frame frame, ConnectionParameters parameters) {
			base.Encode(frame, parameters);

			frame.AppendBytes (scaledValue.GetEncodedValue ());

			frame.SetNextByte (qpm);
		}
	}

	public class ParameterFloatValue : InformationObject
	{

		private float value;

		public float Value {
			get {
				return this.value;
			}
		}

		private byte qpm;

		public float QPM {
			get {
				return qpm;
			}
		}

		public ParameterFloatValue (int objectAddress, float value, byte qpm) :
			base (objectAddress)
		{
			this.value = value;

			this.qpm = qpm;
		}

		public ParameterFloatValue (ConnectionParameters parameters, byte[] msg, int startIndex) :
			base(parameters, msg, startIndex)
		{
			startIndex += parameters.SizeOfIOA; /* skip IOA */

			/* parse float value */
			value = System.BitConverter.ToSingle (msg, startIndex);
			startIndex += 4;

			/* parse QDS (quality) */
			qpm = msg [startIndex++];
		}

		public override void Encode(Frame frame, ConnectionParameters parameters) {
			base.Encode(frame, parameters);

			byte[] floatEncoded = BitConverter.GetBytes (value);

			if (BitConverter.IsLittleEndian == false)
				Array.Reverse (floatEncoded);

			frame.AppendBytes (floatEncoded);

			frame.SetNextByte (qpm);
		}

	}

	public class ParameterActivation : InformationObject
	{

		private byte qpa;

		public static byte NOT_USED = 0;
		public static byte DE_ACT_PREV_LOADED_PARAMETER = 1;
		public static byte DE_ACT_OBJECT_PARAMETER = 2;
		public static byte DE_ACT_OBJECT_TRANSMISSION= 3;

		/// <summary>
		/// Gets the Qualifier of Parameter Activation (QPA)
		/// </summary>
		/// <value>The QP.</value>
		public float QPA {
			get {
				return qpa;
			}
		}

		public ParameterActivation (int objectAddress, byte qpa) :
			base (objectAddress)
		{
			this.qpa = qpa;
		}

		public ParameterActivation (ConnectionParameters parameters, byte[] msg, int startIndex) :
			base(parameters, msg, startIndex)
		{
			startIndex += parameters.SizeOfIOA; /* skip IOA */

			/* parse QDS (quality) */
			qpa = msg [startIndex++];
		}

		public override void Encode(Frame frame, ConnectionParameters parameters) {
			base.Encode(frame, parameters);

			frame.SetNextByte (qpa);
		}

	}


}
