import 'package:flutter/foundation.dart';

// Base URL for LoginApi (port 5142 is the http profile in launchSettings.json).
// Override with: flutter run --dart-define=API_BASE=http://192.168.1.20:5142
const _apiBaseOverride = String.fromEnvironment('API_BASE');

// Android emulator -> host machine is 10.0.2.2; iOS sim / desktop / web -> 127.0.0.1
final String apiBase = _apiBaseOverride.isNotEmpty
    ? _apiBaseOverride
    : (!kIsWeb && defaultTargetPlatform == TargetPlatform.android)
        ? 'http://10.0.2.2:5142'
        : 'http://127.0.0.1:5142';
