import 'package:mobil_application/Model/Enums/room_status.dart';

class RoomModel {
  final int number;
  final String name;
  RoomStatus status;

  RoomModel({required this.number, required this.name, required this.status});

  // the API stores the status as the enum name
  factory RoomModel.fromJson(Map<String, dynamic> json) => RoomModel(
        number: json['number'] as int,
        name: json['name'] as String,
        status: RoomStatus.values.byName(json['status'] as String),
      );
}
