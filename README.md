# HindiVoiceStudio

Cloud-based Hindi and Marathi TTS voice comparison studio. It uses **Sarvam Bulbul v3** and does not require local models or GPU resources.

## Features

- Hindi and Marathi modes
- 5 sample sentences per language, plus custom text
- Compare four voices side by side
- Generate each voice individually or all voices together
- In-browser audio playback
- API key stays in the .NET backend

## Run locally

### 1. Configure the Sarvam API key

Get a key from the Sarvam dashboard and set it as an environment variable.

PowerShell:
```powershell
$env:SARVAM_API_KEY="your-api-key"
```

macOS/Linux:
```bash
export SARVAM_API_KEY="your-api-key"
```

### 2. Start the API

```bash
cd backend/HindiVoiceStudio.Api
dotnet run --urls http://localhost:5000
```

### 3. Open the UI

Open `index.html` in a browser. The default API URL is `http://localhost:5000`.

If the API is deployed elsewhere, open browser DevTools and set:
```js
localStorage.setItem('hvs_api_base', 'https://your-api-host');
``
Then refresh.

## Deploy

### Backend (Render)

Create a Web Service from this repository:
- Root directory: `backend/HindiVoiceStudio.Api`
- Build command: `dotnet publish -c Release -o out`
- Start command: `dotnet out/HindiVoiceStudio.Api.dll`
- Environment variable: `SARVAM_API_KEY`
- Optional: `ALLOWED_ORIGINS` comma-separated allowed frontend origins

Set `ASPNETCORE_URLS` to `http://0.0.0.0:$PORT` if required by your hosting platform.

**For production, set `ALLOWED_ORIGINS` to your actual frontend domain(s)** instead of leaving CORS open.

### Frontend

Deploy `index.html` as a static site (Render Static Site, GitHub Pages, or similar). Configure the API base URL in local storage as shown above, or update `API_BASE` in the page.

## API

`POST /api/tts`

Request:
```json
{
  "text": "नमस्ते!",
  "languageCode": "hi-IN",
  "speaker": "priya"
}
```

Returns a WAV audio file. The API validates the language and speaker, proxies the request to Sarvam, and never exposes the subscription key to the browser.

## Voices

- Hindi: Shubh, Ashutosh, Priya, Suhani
- Marathi: Ratan, Priya, Ritu, Shubh (alternate comparison)

Voice recommendations follow Sarvam's language guidance; compare samples and choose the voices you prefer.

## Provider docs

- [Sarvam TTS REST API](https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/rest-api)
- [Sarvam voices](https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/how-to/change-the-speaker-voice)

## License

MIT
