using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WpfBlueTooth
{
    public static class Util
    {
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> act)
        {
            var tcs = new TaskCompletionSource<TResult>();
            act.Completed += delegate
            {
                switch (act.Status)
                {
                    case AsyncStatus.Completed:
                        tcs.TrySetResult(act.GetResults());
                        break;
                    case AsyncStatus.Error:
                        tcs.TrySetException(act.ErrorCode);
                        break;
                    case AsyncStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                }
            };
            return tcs.Task;
        }

        public static string ReadAsString(this IBuffer buffer)
        {
            if (buffer != null)
            {
                var reader = DataReader.FromBuffer(buffer);
                var input = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(input);
                return BitConverter.ToString(input);
            }
            return "";
        }

        public static Task WriteString(this GattCharacteristic ch, string str )
        {
            var writer = new DataWriter();
            writer.WriteBytes(Encoding.ASCII.GetBytes(str));
            return ch.WriteValueAsync(writer.DetachBuffer()).AsTask();
        }

        public static async Task<string> ReadString(this GattCharacteristic ch)
        {
            var rt = await ch.ReadValueAsync().AsTask();
            if (rt.Value == null) return null;
            return rt.Value.ReadAsString();
        }
    }
}
