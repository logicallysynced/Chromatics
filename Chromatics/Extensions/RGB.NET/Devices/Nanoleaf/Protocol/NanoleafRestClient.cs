using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol
{
    // OpenAPI control plane. One instance per controller. All calls target
    // http://{host}:{port}/api/v1/{token}/... except AddUser (pairing),
    // which has no token yet. HttpClient is shared process-wide; the token
    // and host are per-instance.
    //
    // Every mutating call retries three times with backoff (the v4.3.13
    // pattern) because a controller under load can drop a request; the data
    // plane (UDP frames) never retries because the next frame self-corrects.
    public sealed class NanoleafRestClient
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

        private readonly string _baseUrl;   // http://host:port
        private readonly string _token;

        public NanoleafRestClient(string host, int port, string token)
        {
            _baseUrl = $"http://{host}:{port}";
            _token = token;
        }

        private string Api => $"{_baseUrl}/api/v1/{_token}";

        // Pairing: POST /api/v1/new during the controller's pairing window
        // (user held the power button). Returns the auth token, or null if
        // the window isn't open (403 / non-success). No token required.
        public static async Task<string> PairAsync(string host, int port, CancellationToken ct = default)
        {
            try
            {
                using var resp = await Http.PostAsync($"http://{host}:{port}/api/v1/new", null, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var token = JObject.Parse(body)["auth_token"]?.ToString();
                return string.IsNullOrEmpty(token) ? null : token;
            }
            catch
            {
                return null;
            }
        }

        // GET the full controller state and normalise it into NanoleafState.
        // Also pulls the panel layout in the same round-trip (panelLayout is
        // a nested object of the all-state response). Returns null on any
        // failure so callers treat the controller as unreachable.
        public async Task<NanoleafState> GetStateAsync(CancellationToken ct = default)
        {
            try
            {
                using var resp = await Http.GetAsync(Api, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return ParseState(body);
            }
            catch
            {
                return null;
            }
        }

        public static NanoleafState ParseState(string json)
        {
            var root = JObject.Parse(json);
            var state = new NanoleafState
            {
                Name = root["name"]?.ToString(),
                Model = root["model"]?.ToString(),
                FirmwareVersion = root["firmwareVersion"]?.ToString(),
                SerialNo = root["serialNo"]?.ToString(),
                On = root["state"]?["on"]?["value"]?.Value<bool>() ?? false,
                Brightness = root["state"]?["brightness"]?["value"]?.Value<int>() ?? 0,
                SelectedEffect = root["effects"]?["select"]?.ToString() ?? "",
                GlobalOrientation = root["panelLayout"]?["globalOrientation"]?["value"]?.Value<int>() ?? 0,
            };

            var positions = root["panelLayout"]?["layout"]?["positionData"] as JArray;
            if (positions != null)
            {
                foreach (var p in positions)
                {
                    state.Panels.Add(new NanoleafPanelPosition
                    {
                        PanelId = p["panelId"]?.Value<int>() ?? 0,
                        X = p["x"]?.Value<int>() ?? 0,
                        Y = p["y"]?.Value<int>() ?? 0,
                        O = p["o"]?.Value<int>() ?? 0,
                        ShapeType = p["shapeType"]?.Value<int>() ?? 0,
                    });
                }
            }

            return state;
        }

        // Enter extControl v2 streaming mode. Returns the UDP host+port the
        // controller will accept frames on. The write API version request
        // negotiates v2; the response carries the streaming endpoint.
        public async Task<NanoleafStreamInfo> EnableStreamingAsync(CancellationToken ct = default)
        {
            var payload = new JObject
            {
                ["write"] = new JObject
                {
                    ["command"] = "display",
                    ["animType"] = "extControl",
                    ["extControlVersion"] = "v2",
                },
            };

            return await SendWithRetryAsync<NanoleafStreamInfo>(HttpMethod.Put, $"{Api}/effects", payload.ToString(), body =>
            {
                if (string.IsNullOrEmpty(body)) return new NanoleafStreamInfo { Host = HostOnly(), Port = 60222, ProtocolVersion = 2 };
                var o = JObject.Parse(body);
                return new NanoleafStreamInfo
                {
                    Host = o["streamControlIpAddr"]?.ToString() ?? HostOnly(),
                    Port = o["streamControlPort"]?.Value<int>() ?? 60222,
                    ProtocolVersion = 2,
                };
            }, ct).ConfigureAwait(false);
        }

        // True when the controller is currently in extControl mode. Used by
        // the streaming watchdog: a reboot or a competing app kicks the
        // controller out of streaming and this returns false, prompting a
        // re-enter.
        public async Task<bool> IsStreamingAsync(CancellationToken ct = default)
        {
            var state = await GetSelectedEffectRawAsync(ct).ConfigureAwait(false);
            return string.Equals(state, "*ExtControl*", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GetSelectedEffectRawAsync(CancellationToken ct)
        {
            try
            {
                using var resp = await Http.GetAsync($"{Api}/effects/select", ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return body?.Trim().Trim('"');
            }
            catch
            {
                return null;
            }
        }

        // Restore: re-select a scene by name.
        public Task<bool> SelectEffectAsync(string effectName, CancellationToken ct = default)
        {
            var payload = new JObject { ["select"] = effectName }.ToString();
            return SendWithRetryBoolAsync(HttpMethod.Put, $"{Api}/effects", payload, ct);
        }

        public Task<bool> SetOnAsync(bool on, CancellationToken ct = default)
        {
            var payload = new JObject { ["on"] = new JObject { ["value"] = on } }.ToString();
            return SendWithRetryBoolAsync(HttpMethod.Put, $"{Api}/state", payload, ct);
        }

        public Task<bool> SetBrightnessAsync(int brightness, CancellationToken ct = default)
        {
            var payload = new JObject { ["brightness"] = new JObject { ["value"] = Math.Clamp(brightness, 0, 100) } }.ToString();
            return SendWithRetryBoolAsync(HttpMethod.Put, $"{Api}/state", payload, ct);
        }

        // Read just the power state, for the restore verify-repair step.
        // Narrow endpoint on purpose: the all-state GET parses the full
        // panel layout to read one bool, which matters on 50+ panel walls.
        public async Task<bool?> GetOnAsync(CancellationToken ct = default)
        {
            try
            {
                using var resp = await Http.GetAsync($"{Api}/state/on", ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return JObject.Parse(body)["value"]?.Value<bool>();
            }
            catch
            {
                return null;
            }
        }

        private string HostOnly()
        {
            var uri = new Uri(_baseUrl);
            return uri.Host;
        }

        // ── retry plumbing ──────────────────────────────────────────────

        private async Task<T> SendWithRetryAsync<T>(HttpMethod method, string url, string json, Func<string, T> parse, CancellationToken ct)
        {
            Exception last = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var req = new HttpRequestMessage(method, url);
                    if (json != null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                        return parse(body);
                    }
                    last = new HttpRequestException($"HTTP {(int)resp.StatusCode}");
                }
                catch (Exception ex)
                {
                    last = ex;
                }
                if (attempt < 2) await Task.Delay(250 * (attempt + 1), ct).ConfigureAwait(false);
            }
            throw last ?? new HttpRequestException("Nanoleaf request failed");
        }

        private async Task<bool> SendWithRetryBoolAsync(HttpMethod method, string url, string json, CancellationToken ct)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var req = new HttpRequestMessage(method, url);
                    if (json != null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch
                {
                    // fall through to retry
                }
                if (attempt < 2) await Task.Delay(250 * (attempt + 1), ct).ConfigureAwait(false);
            }
            return false;
        }
    }
}
