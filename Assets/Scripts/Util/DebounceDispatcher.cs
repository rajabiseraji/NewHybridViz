using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class DebounceDispatcher
{
    public static Action<T> Debounce<T>(this Action<T> action, int milliseconds = 300)
    {
        CancellationTokenSource lastCToken = null;

        return (arg) =>
        {
            //Cancel/dispose previous
            lastCToken?.Cancel();
            try
            {
                lastCToken?.Dispose();
            }
            catch {
                Debug.Log("error in the debounce");
            }

            var tokenSrc = lastCToken = new CancellationTokenSource();

            Task.Delay(milliseconds).ContinueWith(task => { action(arg); }, tokenSrc.Token);
        };
    }
}