using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangoSpot8
{
    class SpotifyModal
    {

        public class TokenResponse
        {
            public string AccessToken { get; set; }
            public string TokenType { get; set; }
            public int ExpiresIn { get; set; }
            public string RefreshToken { get; set; }
            public string Scope { get; set; }
        }

        public class PlaylistItem
        {
            public string Name { get; set; }
            public string TrackCount { get; set; }
            public string Author { get; set; }
            public string PlaylistID { get; set; }
            public string ImageUrl { get; set; }
        }

        public class MediaMetadata
        {
            public string ImageUrl { get; set; }
            public string Author { get; set; }
            public string Title { get; set; }
        }

        public class SpotifySearchResult
        {
            public SpotifyAlbumResponse Albums { get; set; }
            public SpotifyTrackResponse Tracks { get; set; }
        }

        public class SpotifyAlbumResponse
        {
            public List<SpotifyAlbum> Items { get; set; }
        }

        public class SpotifyTrackResponse
        {
            public List<SpotifyTrack> Items { get; set; }
        }

        public class SpotifyAlbum
        {
            public string Name { get; set; }
            public List<SpotifyArtist> Artists { get; set; }
            public List<SpotifyImage> Images { get; set; }
            public string Id { get; internal set; }
        }

        public class SpotifyTrack
        {
            public string Name { get; set; }
            public SpotifyAlbum Album { get; set; }
            public List<SpotifyArtist> Artists { get; set; }
            public string Id { get; internal set; }
        }

        public class SpotifyArtist
        {
            public string Name { get; set; }
        }

        public class SpotifyImage
        {
            public string Url { get; set; }
        }

        public class SongItem
        {
            public string Name { get; set; }
            public string Author { get; set; }
            public string SongName { get; set; }
            public string SongId { get; set; }
        }

        public class AlbumItem
        {
            public string AlbumID { get; set; }
            public string Name { get; set; }
            public string Author { get; set; }
            public string ImageUrl { get; set; }
        }

        public class SearchData
        {
            public string Query { get; set; }
            public Context Context { get; set; }
            public string Params { get; set; }
        }

        public class Context
        {
            public Client Client { get; set; }
        }

        public class Client
        {
            public string Hl { get; set; }
            public string Gl { get; set; }
            public string ClientName { get; set; }
            public string ClientVersion { get; set; }
        }

        public class TrackMetadata
        {
            public string SongID { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string ThumbnailUrl { get; set; }
        }


    }
}
