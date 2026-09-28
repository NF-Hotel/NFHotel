import 'package:flutter/material.dart';

import '../Authentication/auth_session.dart';
import '../Repositories/note_repository.dart';
import '../theme.dart';

const _editWindow = Duration(minutes: 10);

class RoomNotes extends StatefulWidget {
  final int roomNumber;

  const RoomNotes({super.key, required this.roomNumber});

  @override
  State<RoomNotes> createState() => _RoomNotesState();
}

class _RoomNotesState extends State<RoomNotes> {
  final _repo = NoteRepository();
  final _controller = TextEditingController();
  List<RoomNote> _notes = [];
  bool _loading = true;
  bool _posting = false;

  @override
  void initState() {
    super.initState();
    _loadNotes();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _showError(String message) =>
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));

  Future<void> _loadNotes() async {
    final res = await _repo.fetchNotes(widget.roomNumber);
    if (!mounted) return;
    setState(() {
      if (res.statusCode == 200) _notes = _repo.parseNotes(res.body);
      _loading = false;
    });
    if (res.statusCode != 200) {
      _showError('Could not load notes (${res.statusCode})');
    }
  }

  Future<void> _postNote() async {
    final text = _controller.text.trim();
    if (text.isEmpty) return;

    setState(() => _posting = true);
    final res = await _repo.addNote(widget.roomNumber, text);
    if (!mounted) return;
    setState(() => _posting = false);

    if (res.statusCode == 200) {
      _controller.clear();
      _loadNotes();
    } else {
      _showError('Could not post note');
    }
  }

  Future<bool> _confirm(String title, String content, String action) async =>
      await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: Text(title),
          content: Text(content),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, true),
              child: Text(action),
            ),
          ],
        ),
      ) ==
      true;

  Future<void> _setResolved(RoomNote note, bool resolved) async {
    if (!resolved &&
        !await _confirm(
          'Untick note',
          'Mark "${note.body}" as not fixed?',
          'Untick',
        )) {
      return;
    }
    final res = await _repo.setResolved(widget.roomNumber, note.id, resolved);
    if (!mounted) return;
    if (res.statusCode != 200) {
      _showError(
        res.statusCode == 403
            ? 'Only the person who ticked it (within 10 minutes) or an admin can untick'
            : 'Could not update note',
      );
    }
    _loadNotes();
  }

  Future<void> _deleteNote(RoomNote note) async {
    if (!await _confirm(
      'Delete note',
      'Delete "${note.body}"? This cannot be undone.',
      'Delete',
    )) {
      return;
    }
    final res = await _repo.deleteNote(widget.roomNumber, note.id);
    if (!mounted) return;
    if (res.statusCode != 200) {
      _showError(
        res.statusCode == 403
            ? 'Only the author (within 10 minutes) or an admin can delete'
            : 'Could not delete note',
      );
    }
    _loadNotes();
  }

  // UI hints only; the API enforces the same windows
  bool _deletable(RoomNote note) =>
      AuthSession.isAdmin ||
      (note.createdBy == AuthSession.email &&
          DateTime.now().difference(note.createdAt) <= _editWindow);

  bool _locked(RoomNote note) =>
      note.resolved &&
      !AuthSession.isAdmin &&
      (note.resolvedBy != AuthSession.email ||
          DateTime.now().difference(note.resolvedAt!) > _editWindow);

  String _formatTime(DateTime time) {
    final l10n = MaterialLocalizations.of(context);
    return '${l10n.formatShortDate(time)} ${l10n.formatTimeOfDay(TimeOfDay.fromDateTime(time))}';
  }

  Widget _noteTile(RoomNote note) {
    final small = Theme.of(context).textTheme.bodySmall;
    return CheckboxListTile(
      controlAffinity: ListTileControlAffinity.leading,
      contentPadding: EdgeInsets.zero,
      activeColor: AppColors.navy,
      value: note.resolved,
      onChanged: _locked(note) ? null : (value) => _setResolved(note, value!),
      secondary: _deletable(note)
          ? IconButton(
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Delete note',
              onPressed: () => _deleteNote(note),
            )
          : null,
      title: Text(
        note.body,
        style: note.resolved
            ? const TextStyle(decoration: TextDecoration.lineThrough)
            : null,
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '${note.createdBy} · ${_formatTime(note.createdAt)}',
            style: small,
          ),
          if (note.resolved)
            Row(
              children: [
                const Icon(Icons.fingerprint, size: 16, color: AppColors.navy),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    'Fixed by ${note.resolvedBy ?? 'deleted user'} · ${_formatTime(note.resolvedAt!)}',
                    style: small,
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Notes', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            TextField(
              controller: _controller,
              minLines: 1,
              maxLines: 5,
              textCapitalization: TextCapitalization.sentences,
              decoration: const InputDecoration(hintText: 'Write a note…'),
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerRight,
              child: ElevatedButton(
                onPressed: _posting ? null : _postNote,
                child: const Text('Post'),
              ),
            ),
            const SizedBox(height: 8),
            if (_loading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_notes.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 8),
                child: Text('No notes yet.'),
              )
            else
              for (final note in _notes) ...[
                const Divider(height: 1),
                _noteTile(note),
              ],
          ],
        ),
      ),
    );
  }
}
