// Nx release version actions for an MSBuild solution whose packages share one <Version> in Directory.Build.props.
// The root project is the only release project, so there are no inter-project dependencies to update.

const { VersionActions } = require('nx/release');

const versionPattern = /<Version>([^<]*)<\/Version>/;

function readVersion(tree, manifestPath) {
	const match = tree.read(manifestPath, 'utf-8').match(versionPattern);
	if (!match) {
		throw new Error(`Could not find a <Version> element in ${manifestPath}`);
	}
	return match[1];
}

function writeVersion(tree, manifestPath, newVersion) {
	const contents = tree.read(manifestPath, 'utf-8');
	tree.write(
		manifestPath,
		contents.replace(versionPattern, `<Version>${newVersion}</Version>`),
	);
}

const readmePath = 'README.md';

// The download links of the README, which name the release's files, as `oldVersion` that is not part of a longer version
// (0.0.1-alpha.1 in 0.0.1-alpha.16).
function writeReadmeVersion(tree, oldVersion, newVersion) {
	const escaped = oldVersion.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
	const contents = tree.read(readmePath, 'utf-8');
	tree.write(
		readmePath,
		contents.replace(new RegExp(`(?<![\\d.])${escaped}(?!\\d)`, 'g'), newVersion),
	);
}

class MSBuildVersionActions extends VersionActions {
	validManifestFilenames = ['Directory.Build.props'];

	async readCurrentVersionFromSourceManifest(tree) {
		const { manifestPath } = this.manifestsToUpdate[0];
		return { currentVersion: readVersion(tree, manifestPath), manifestPath };
	}

	async readCurrentVersionFromRegistry() {
		return null;
	}

	async readCurrentVersionOfDependency() {
		return { currentVersion: null, dependencyCollection: null };
	}

	async updateProjectVersion(tree, newVersion) {
		const oldVersion = readVersion(tree, this.manifestsToUpdate[0].manifestPath);
		const logs = this.manifestsToUpdate.map(({ manifestPath }) => {
			writeVersion(tree, manifestPath, newVersion);
			return `✍️  New version ${newVersion} written to manifest: ${manifestPath}`;
		});
		writeReadmeVersion(tree, oldVersion, newVersion);
		return [...logs, `✍️  New version ${newVersion} written to the download links of ${readmePath}`];
	}

	async updateProjectDependencies() {
		return [];
	}
}

module.exports = MSBuildVersionActions;
