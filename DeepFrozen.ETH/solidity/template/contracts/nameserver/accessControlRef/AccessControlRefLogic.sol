// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AccessControlRefLayout.sol";
import "./AccessControlRefInterface.sol";

import "../../nameserver/accessControl/AccessControlInterface.sol";

contract AccessControlRefLogic is AccessControlRefLayout, AccessControlRefInterface {

    function ac() view internal returns (AccessControlInterface){
        return AccessControlInterface(_accessControl);
    }

    function accessControl() override view external returns (address){
        return address(_accessControl);
    }

    function _setAccessControl(address accessControl_) internal {
        _accessControl = accessControl_;
    }

}
