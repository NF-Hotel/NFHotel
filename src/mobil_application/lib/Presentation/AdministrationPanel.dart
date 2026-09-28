import 'package:flutter/material.dart';

import '../Repositories/user_repository.dart';

class AdministrationPanel extends StatefulWidget {
  const AdministrationPanel({super.key});

  @override
  _AdministrationPanelState createState() => _AdministrationPanelState();
}

const _roles = ['admin', 'cleaner', 'technician'];

class _AdministrationPanelState extends State<AdministrationPanel> {
  final _repo = UserRepository();
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _creating = false;

  List<AdminUser> _users = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadUsers();
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _createAccount() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _creating = true);
    final res = await _repo.register(
      _firstNameController.text.trim(),
      _lastNameController.text.trim(),
      _emailController.text,
      _passwordController.text,
    );
    if (!mounted) return;
    setState(() => _creating = false);

    if (res.statusCode == 200) {
      _firstNameController.clear();
      _lastNameController.clear();
      _emailController.clear();
      _passwordController.clear();
      _loadUsers();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            res.statusCode == 409
                ? 'Email already registered'
                : 'Could not create account',
          ),
        ),
      );
    }
  }

  Future<void> _loadUsers() async {
    setState(() => _loading = true);
    final res = await _repo.fetchUsers();
    if (!mounted) return;
    if (res.statusCode == 200) {
      setState(() {
        _users = _repo.parseUsers(res.body);
        _loading = false;
      });
    } else {
      setState(() => _loading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Could not load users (${res.statusCode})')),
      );
    }
  }

  Future<void> _editName(AdminUser user) async {
    final firstController = TextEditingController(text: user.firstName);
    final lastController = TextEditingController(text: user.lastName);
    final saved = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Edit name'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: firstController,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(labelText: 'First name'),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: lastController,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(labelText: 'Last name'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Save'),
          ),
        ],
      ),
    );
    final firstName = firstController.text.trim();
    final lastName = lastController.text.trim();
    if (saved != true ||
        (firstName == user.firstName && lastName == user.lastName)) {
      return;
    }
    if (!mounted) return;
    if (firstName.isEmpty || lastName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('First and last name are required')),
      );
      return;
    }

    final res = await _repo.updateName(user.id, firstName, lastName);
    if (!mounted) return;
    if (res.statusCode == 200) {
      _loadUsers();
    } else {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Could not update name')));
    }
  }

  Future<void> _editEmail(AdminUser user) async {
    final controller = TextEditingController(text: user.email);
    final newEmail = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Edit email'),
        content: TextField(
          controller: controller,
          keyboardType: TextInputType.emailAddress,
          decoration: const InputDecoration(labelText: 'Email'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, controller.text),
            child: const Text('Save'),
          ),
        ],
      ),
    );
    if (newEmail == null || newEmail == user.email) return;

    final res = await _repo.updateEmail(user.id, newEmail);
    if (!mounted) return;
    if (res.statusCode == 200) {
      _loadUsers();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            res.statusCode == 409
                ? 'Email already in use'
                : 'Could not update email',
          ),
        ),
      );
    }
  }

  Future<void> _editPassword(AdminUser user) async {
    final controller = TextEditingController();
    final newPassword = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Change password for ${user.email}'),
        content: TextField(
          controller: controller,
          obscureText: true,
          decoration: const InputDecoration(labelText: 'New password'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, controller.text),
            child: const Text('Save'),
          ),
        ],
      ),
    );
    if (newPassword == null || newPassword.isEmpty) return;

    final res = await _repo.updatePassword(user.id, newPassword);
    if (!mounted) return;
    if (res.statusCode != 200) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not update password')),
      );
    }
  }

  Future<void> _editRole(AdminUser user) async {
    final newRole = await showDialog<String>(
      context: context,
      builder: (context) => SimpleDialog(
        title: Text('Change role for ${user.email}'),
        children: _roles
            .map(
              (role) => SimpleDialogOption(
                onPressed: () => Navigator.pop(context, role),
                child: Text(role),
              ),
            )
            .toList(),
      ),
    );
    if (newRole == null || newRole == user.role) return;

    final res = await _repo.updateRole(user.id, newRole);
    if (!mounted) return;
    if (res.statusCode == 200) {
      _loadUsers();
    } else {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Could not update role')));
    }
  }

  Future<void> _deleteUser(AdminUser user) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete account'),
        content: Text('Delete ${user.email}? This cannot be undone.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final res = await _repo.deleteUser(user.id);
    if (!mounted) return;
    if (res.statusCode == 200) {
      _loadUsers();
    } else {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Could not delete account')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Administration')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
            padding: const EdgeInsets.all(16),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Create account', style: textTheme.headlineSmall),
                  const SizedBox(height: 8),
                  Text(
                    'Set up staff access to manage rooms.',
                    style: textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            TextFormField(
                              controller: _firstNameController,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'First name',
                              ),
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _lastNameController,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'Last name',
                              ),
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'Email',
                              ),
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _passwordController,
                              decoration: const InputDecoration(
                                labelText: 'Password',
                              ),
                              obscureText: true,
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 24),
                            ElevatedButton(
                              onPressed: _creating ? null : _createAccount,
                              child: _creating
                                  ? const SizedBox(
                                      height: 20,
                                      width: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : const Text('Sign Up'),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 32),
                  Text('Users', style: textTheme.headlineSmall),
                  const SizedBox(height: 8),
                  if (_loading)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Center(child: CircularProgressIndicator()),
                    )
                  else if (_users.isEmpty)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 16),
                      child: Text('No other users.'),
                    )
                  else
                    Card(
                      child: ListView.separated(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: _users.length,
                        separatorBuilder: (_, __) => const Divider(height: 1),
                        itemBuilder: (context, i) {
                          final user = _users[i];
                          return ListTile(
                            title: Text(user.fullName),
                            subtitle: Text('${user.email}\n${user.role}'),
                            isThreeLine: true,
                            // one menu instead of 4 icon buttons, which left no room for the email on phones
                            trailing: PopupMenuButton<VoidCallback>(
                              tooltip: 'Actions',
                              onSelected: (action) => action(),
                              itemBuilder: (context) => [
                                PopupMenuItem(
                                  value: () => _editName(user),
                                  child: const ListTile(
                                    leading: Icon(Icons.person),
                                    title: Text('Edit name'),
                                  ),
                                ),
                                PopupMenuItem(
                                  value: () => _editEmail(user),
                                  child: const ListTile(
                                    leading: Icon(Icons.edit),
                                    title: Text('Edit email'),
                                  ),
                                ),
                                PopupMenuItem(
                                  value: () => _editPassword(user),
                                  child: const ListTile(
                                    leading: Icon(Icons.lock_reset),
                                    title: Text('Change password'),
                                  ),
                                ),
                                PopupMenuItem(
                                  value: () => _editRole(user),
                                  child: const ListTile(
                                    leading: Icon(Icons.badge),
                                    title: Text('Change role'),
                                  ),
                                ),
                                PopupMenuItem(
                                  value: () => _deleteUser(user),
                                  child: const ListTile(
                                    leading: Icon(
                                      Icons.delete,
                                      color: Colors.red,
                                    ),
                                    title: Text('Delete account'),
                                  ),
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
