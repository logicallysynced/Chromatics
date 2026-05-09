using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue
{
    // Locates Hue bridges on the user's network without forcing a manual
    // IP entry. Two strategies, attempted in this order:
    //
    //   1. Philips' cloud N-UPnP endpoint (https://discovery.meethue.com/).
    //      Returns the bridges Philips' broker has seen come from the
    //      caller's public IP. Works through NAT, requires internet, and
    //      finishes in <1s in the typical case. This is the same endpoint
    //      the official Hue app uses on first launch.
    //
    //   2. (Future) mDNS / SSDP local-network probe. Adds an offline path
    //      for users without internet egress to the discovery host. Not
    //      implemented in v4.1.31 — the cloud endpoint covers >99% of
    //      home setups; mDNS becomes the fallback when we surface a real
    //      "no internet" complaint.
    //
    // Manual IP entry is always available in HueBridgeDialog as a third
    // path. Discovery is best-effort — the dialog proceeds with manual
    // entry whether or not auto-discovery returns hits.
    public static class HueBridgeDiscovery
    {
        public sealed class DiscoveredBridge
        {
            public string Id { get; init; }
            public string InternalIp { get; init; }
            public int Port { get; init; }

            public string DisplayLabel =>
                string.IsNullOrEmpty(Id) ? InternalIp : $"{InternalIp}  ·  {Id}";
        }

        // Single shared HttpClient — repeated dialog opens shouldn't blow
        // through the socket pool. ~3s timeout: bridges respond in <500ms
        // when present, but on a slow / captive-portal connection we want
        // the dialog to fall back to manual entry quickly rather than
        // freeze the UI.
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3),
        };

        private const string DiscoveryEndpoint = "https://discovery.meethue.com/";

        public static async Task<IReadOnlyList<DiscoveredBridge>> DiscoverAsync(CancellationToken ct = default)
        {
            try
            {
                using var resp = await _http.GetAsync(DiscoveryEndpoint, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return Array.Empty<DiscoveredBridge>();

                var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<DiscoveredBridge>();

                var list = new List<DiscoveredBridge>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    string id = element.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                        ? idEl.GetString()
                        : null;
                    string ip = element.TryGetProperty("internalipaddress", out var ipEl) && ipEl.ValueKind == JsonValueKind.String
                        ? ipEl.GetString()
                        : null;
                    int port = element.TryGetProperty("port", out var portEl) && portEl.TryGetInt32(out var p)
                        ? p
                        : 443;

                    if (string.IsNullOrEmpty(ip)) continue;
                    list.Add(new DiscoveredBridge { Id = id, InternalIp = ip, Port = port });
                }
                return list;
            }
            catch (TaskCanceledException) { return Array.Empty<DiscoveredBridge>(); }
            catch (HttpRequestException) { return Array.Empty<DiscoveredBridge>(); }
            catch (JsonException) { return Array.Empty<DiscoveredBridge>(); }
            catch (Exception) { return Array.Empty<DiscoveredBridge>(); }
        }
    }
}
