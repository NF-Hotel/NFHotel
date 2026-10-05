import 'dart:convert';

import 'package:http/http.dart' as http;

import '../Authentication/auth_session.dart';
import '../Model/Enums/room_status.dart';
import '../Model/roomModel.dart';
import '../api_config.dart';

/// Thrown by [RoomRepository] when the API rejects a request; [message] is
/// safe to show to the user as-is.
class RoomRepositoryException implements Exception {
  final String message;
  RoomRepositoryException(this.message);

  @override
  String toString() => message;
}

class RoomRepository {
  Map<String, String> get _authHeaders => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthSession.token}',
      };

  /// All rooms, or only the assigned ones when logged in as a cleaner.
  Future<List<RoomModel>> fetchRooms() async {
    final res =
        await http.get(Uri.parse('$apiBase/rooms'), headers: _authHeaders);
    if (res.statusCode != 200) {
      throw RoomRepositoryException('Could not load rooms (${res.statusCode})');
    }
    return (jsonDecode(res.body) as List)
        .map((e) => RoomModel.fromJson(e))
        .toList();
  }

  void _ensureOk(http.Response res, String message) {
    if (res.statusCode == 200) return;
    if (res.statusCode == 409) {
      throw RoomRepositoryException('Room name already in use');
    }
    throw RoomRepositoryException(message);
  }

  Future<void> addRoom(String name) async {
    final res = await http.post(
      Uri.parse('$apiBase/admin/rooms'),
      headers: _authHeaders,
      body: jsonEncode({'name': name.trim()}),
    );
    _ensureOk(res, 'Could not add room');
  }

  Future<void> renameRoom(int room, String name) async {
    final res = await http.put(
      Uri.parse('$apiBase/admin/rooms/$room/name'),
      headers: _authHeaders,
      body: jsonEncode({'name': name.trim()}),
    );
    _ensureOk(res, 'Could not rename room');
  }

  Future<void> deleteRoom(int room) async {
    final res = await http.delete(
      Uri.parse('$apiBase/admin/rooms/$room'),
      headers: _authHeaders,
    );
    _ensureOk(res, 'Could not delete room');
  }

  Future<void> updateStatus(int room, RoomStatus status) async {
    final res = await http.put(
      Uri.parse('$apiBase/rooms/$room/status'),
      headers: _authHeaders,
      body: jsonEncode({'status': status.name}),
    );
    if (res.statusCode == 403) {
      throw RoomRepositoryException('This room is not assigned to you');
    }
    if (res.statusCode != 200) {
      throw RoomRepositoryException('Could not update room');
    }
  }
}
