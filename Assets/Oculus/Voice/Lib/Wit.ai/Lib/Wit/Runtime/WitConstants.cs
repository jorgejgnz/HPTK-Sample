/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi
{
    public static class WitConstants
    {
        // Wit service version info
        public const string API_VERSION = "20220728";
        public const string SDK_VERSION = "49.0.50";
        public const string CLIENT_NAME = "wit-unity";

        // Wit service endpoint info
        public const string URI_SCHEME = "https";
        public const string URI_AUTHORITY = "api.wit.ai";
        public const int URI_DEFAULT_PORT = -1;
        // Wit service header keys
        public const string HEADER_REQUEST_ID = "X-Wit-Client-Request-Id";
        public const string HEADER_AUTH = "Authorization";
        public const string HEADER_USERAGENT = "User-Agent";
        public const string HEADER_USERAGENT_PREFIX = "";
        public const string HEADER_USERAGENT_CONFID_MISSING = "not-yet-configured";
        public const string HEADER_POST_CONTENT = "Content-Type";
        public const string HEADER_GET_CONTENT = "Accept";

        // NLP Endpoints
        public const string ENDPOINT_SPEECH = "speech";
        public const string ENDPOINT_MESSAGE = "message";
        public const string ENDPOINT_MESSAGE_PARAM = "q";
        public const string ENDPOINT_JSON_DELIMITER = "\r\n";

        // TTS Endpoint
        public const string ENDPOINT_TTS = "synthesize";
        public const string ENDPOINT_TTS_PARAM = "q";
        public const string ENDPOINT_TTS_CLIP = "WitTTSClip";
        public const string ENDPOINT_TTS_NO_TEXT = "No text provided";
        public const int ENDPOINT_TTS_TIMEOUT = 10000; // In ms
        public const int ENDPOINT_TTS_MAX_TEXT_LENGTH = 140;

        // Dictation Endpoint
        public const string ENDPOINT_DICTATION = "dictation";

        // Composer Endpoints
        public const string ENDPOINT_COMPOSER_SPEECH = "converse";
        public const string ENDPOINT_COMPOSER_MESSAGE = "event";
        public const string ENDPOINT_COMPOSER_PARAM_SESSION = "session_id";
        public const string ENDPOINT_COMPOSER_PARAM_CONTEXT_MAP = "context_map";

        // Runtime Sync Endpoints
        public const string ENDPOINT_IMPORT = "import";
        public const string ENDPOINT_INTENTS = "intents";
    }
}
