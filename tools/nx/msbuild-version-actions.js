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
		return this.manifestsToUpdate.map(({ manifestPath }) => {
			writeVersion(tree, manifestPath, newVersion);
			return `✍️  New version ${newVersion} written to manifest: ${manifestPath}`;
		});
	}

	async updateProjectDependencies() {
		return [];
	}
}

module.exports = MSBuildVersionActions;
