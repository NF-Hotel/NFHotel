import 'dart:convert';

import 'package:http/http.dart' as http;

import '../Authentication/auth_session.dart';
import '../api_config.dart';

class RoomNote {
  final int id;
  final String body;
  final String createdBy;
  final DateTime createdAt;
  final String? resolvedBy;
  final DateTime? resolvedAt;

  RoomNote({
    required this.id,
    required this.body,
    required this.createdBy,
    required this.createdAt,
    this.resolvedBy,
    this.resolvedAt,
  });

  bool get resolved => resolvedAt != null;

  factory RoomNote.fromJson(Map<String, dynamic> json) => RoomNote(
        id: json['id'] as int,
        body: json['body'] as String,
        createdBy: json['createdBy'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        resolvedBy: json['resolvedBy'] as String?,
        resolvedAt: json['resolvedAt'] == null
            ? null
            : DateTime.parse(json['resolvedAt'] as String).toLocal(),
      );
}

class NoteRepository {
  Map<String, String> get _authHeaders => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthSession.token}',
      };

  Future<http.Response> fetchNotes(int room) =>
      http.get(Uri.parse('$apiBase/rooms/$room/notes'), headers: _authHeaders);

  List<RoomNote> parseNotes(String body) =>
      (jsonDecode(body) as List).map((e) => RoomNote.fromJson(e)).toList();

  Future<http.Response> addNote(int room, String body) => http.post(
        Uri.parse('$apiBase/rooms/$room/notes'),
        headers: _authHeaders,
        body: jsonEncode({'body': body}),
      );

  Future<http.Response> setResolved(int room, int id, bool resolved) => http.put(
        Uri.parse('$apiBase/rooms/$room/notes/$id/resolved'),
        headers: _authHeaders,
        body: jsonEncode({'resolved': resolved}),
      );

  Future<http.Response> deleteNote(int room, int id) => http.delete(
        Uri.parse('$apiBase/rooms/$room/notes/$id'),
        headers: _authHeaders,
      );
}
