using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketListener
{
    internal class Listener
    {
        private static async void AudioRecording()
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:80/");
            httpListener.Start();
            while (true)
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();
                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    WebSocketContext websocketContext;
                    try
                    {
                        websocketContext = await httpListenerContext.AcceptWebSocketAsync(subProtocol: null);
                        string ipAddress = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
                    }
                    catch (Exception ex)
                    {
                        httpListenerContext.Response.StatusCode = 500;
                        httpListenerContext.Response.Close();
                        return;
                    }

                    WebSocket webSocket = websocketContext.WebSocket;
                    try
                    {
                        while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                        {
                            byte[] receiveBuffer = new byte[2048];
                            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                            {
                                var data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                                try
                                {
                                    var json = JsonConvert.DeserializeObject<Audio>(data);
                                    if (json != null)
                                    {
                                        var byteArray = json.AudioData;
                                        //Processing mixed audio data
                                        if (string.IsNullOrEmpty(json?.ParticipantId))
                                        {
                                            if (string.IsNullOrEmpty(WebSocketData.FirstReceivedMixedAudioBufferTimeStamp))
                                            {
                                                WebSocketData.FirstReceivedMixedAudioBufferTimeStamp = json.Timestamp;
                                            }
                                            //Process byteArray ( audioData ) however you want
                                        }
                                    }
                                    //Processing unmixed audio data
                                    else if (!string.IsNullOrEmpty(json?.ParticipantId) && !json.IsSilence)
                                    {
                                        if (json.ParticipantId != null)
                                        {
                                            switch (json.ParticipantId)
                                            {
                                                case { participantRawId1 }:
                                                    //Process audio data
                                                    break;
                                                case { participantRawId2 }::
                                            //Process audio data
break;
                                                default:
                                                    break;
                                            }
                                        }
                                        if (string.IsNullOrEmpty(WebSocketData.FirstReceivedUnmixedAudioBufferTimeStamp))
                                        {
                                            WebSocketData.FirstReceivedUnmixedAudioBufferTimeStamp = json.Timestamp;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                }
            }
        }
    }
