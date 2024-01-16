pipeline {
  agent none
  triggers {
    pollSCM('* * * * *')
  }

  environment {
    UNITY_BUILDER_CLI = '/unity-builder-cli/dist/index.js'
    UNITY_PATH = '/opt/unity/Editor/Unity'
  }

  stages {
    stage("Build Mac") {
      agent {
        docker {
          image 'unity-2022.3.9f1-mac'
        }
      }

      steps {
        sh '''#!/bin/bash -l
          cd /unity-builder-cli
          node $UNITY_BUILDER_CLI build-pipeline --unityPath $UNITY_PATH --projectPath $WORKSPACE --platform StandaloneOSX --username $user_unity --password $password_unity --serial $serial_unity --buildNumber $BUILD_NUMBER
        '''
      }
    }

    stage("Build Windows") {
      agent {
        docker {
          image 'unity-2022.3.9f1-windows'
        }
      }

      steps {
        sh '''#!/bin/bash -l
          cd /unity-builder-cli
          node $UNITY_BUILDER_CLI build-pipeline --unityPath $UNITY_PATH --projectPath $WORKSPACE --platform StandaloneWindows64 --username $user_unity --password $password_unity --serial $serial_unity --buildNumber $BUILD_NUMBER
        '''
      }
    }    
  }
}
