export interface Package {
  id: string;
  version: string;
  description: string;
  authors: string;
  packageDependencies: PackageDependencies[]
}

export interface PackageDependency {
  id: string;
  version: string;
}

export interface PackageDependencies {
  targetFramework: string;
  dependencies: PackageDependency[]
}
