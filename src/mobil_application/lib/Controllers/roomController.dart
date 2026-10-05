import 'package:flutter/foundation.dart';
import 'package:mobil_application/Model/Enums/room_status.dart';
import 'package:mobil_application/Model/roomModel.dart';
import 'package:mobil_application/Repositories/room_repository.dart';

/// Rooms and their status, shared between phones through the API. Actions
/// throw [RoomRepositoryException] on failure.
class RoomController extends ChangeNotifier {
  final _repo = RoomRepository();

  /// Cleaners only get the rooms designated to them; everyone else gets all.
  List<RoomModel> rooms = [];

  /// False until the first load finishes.
  bool loaded = false;

  Future<void> loadRooms() async {
    rooms = await _repo.fetchRooms();
    loaded = true;
    notifyListeners();
  }

  void clear() {
    rooms = [];
    loaded = false;
  }

  Future<void> addRoom(String name) async {
    if (name.trim().isEmpty) return;
    await _repo.addRoom(name);
    await loadRooms();
  }

  Future<void> renameRoom(RoomModel room, String name) async {
    if (name.trim().isEmpty || name.trim() == room.name) return;
    await _repo.renameRoom(room.number, name);
    await loadRooms();
  }

  Future<void> deleteRoom(RoomModel room) async {
    await _repo.deleteRoom(room.number);
    await loadRooms();
  }

  Future<void> setRoomStatus(RoomModel room, RoomStatus status) async {
    if (room.status == status) return;
    await _repo.updateStatus(room.number, status);
    room.status = status;
    notifyListeners();
  }
}

final roomController = RoomController();
