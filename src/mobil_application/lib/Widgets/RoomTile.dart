import 'package:flutter/material.dart';
import 'package:mobil_application/Model/roomModel.dart';
import 'package:mobil_application/theme.dart';

class RoomTile extends StatelessWidget {
  final RoomModel room;
  final VoidCallback? onTap;

  const RoomTile({super.key, required this.room, this.onTap});

  @override
  Widget build(BuildContext context) {
    final icon = room.status.icon;
    final textTheme = Theme.of(context).textTheme;
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(16),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: AppColors.border),
          ),
          padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 8),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SizedBox(
                width: 64,
                height: 64,
                child: icon != null
                    ? Image.asset(icon, fit: BoxFit.contain)
                    : Container(
                        decoration: BoxDecoration(
                          color: const Color(0xFF16A34A),
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: const Icon(Icons.check_rounded, color: Colors.white, size: 32),
                      ),
              ),
              const SizedBox(height: 12),
              Text(
                room.name,
                style: textTheme.titleMedium,
              ),
              const SizedBox(height: 4),
              Text(
                room.status.label,
                textAlign: TextAlign.center,
                style: textTheme.bodyMedium?.copyWith(fontSize: 12, fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
