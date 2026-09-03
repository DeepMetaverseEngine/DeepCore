// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;


abstract contract OwnerProxy {

    string internal _version;

    address internal _implementation;

    address private _upgradeabilityOwner;


    event Upgraded(string version, address indexed implementation);

    event ProxyOwnershipTransferred(address previousOwner, address newOwner);

    constructor() {
        _setUpgradeabilityOwner(msg.sender);
    }

    modifier onlyProxyOwner() {
        require(msg.sender == proxyOwner());
        _;
    }

    function proxyOwner() public view returns (address) {
        return upgradeabilityOwner();
    }

    function transferProxyOwnership(address newOwner) public onlyProxyOwner {
        require(newOwner != address(0));
        emit ProxyOwnershipTransferred(proxyOwner(), newOwner);
        _setUpgradeabilityOwner(newOwner);
    }

    function upgradeTo(string calldata new_version, address new_implementation) public onlyProxyOwner {
        _upgradeTo(new_version, new_implementation);
    }

    function upgradeToAndCall(string calldata new_version, address new_implementation, bytes calldata data) payable public onlyProxyOwner {
        upgradeTo(new_version, new_implementation);
        //require(address(this).call.value(msg.value)(data));
        (bool success,  ) = address(this).call{value: msg.value}(data);
        require(success, "Contract execution Failed");
    }


    function upgradeabilityOwner() public view returns (address) {
        return _upgradeabilityOwner;
    }

    function _setUpgradeabilityOwner(address newUpgradeabilityOwner) internal {
        _upgradeabilityOwner = newUpgradeabilityOwner;
    }


    function _upgradeTo(string calldata new_version, address new_implementation) internal {
        require(_implementation != new_implementation);
        _version = new_version;
        _implementation = new_implementation;
        emit Upgraded(new_version, new_implementation);
    }





    function version() public view returns (string memory)     {
        return _version;
    }

    function implementation() public view returns (address)     {
        return _implementation;
    }


    receive() payable external     {
        _fallback();
    }

    fallback() payable external     {
        _fallback();
    }

    function _fallback() private     {
        address _impl = implementation();
        require(_impl != address(0));
   
        assembly         {
            let ptr := mload(0x40)
            calldatacopy(ptr, 0, calldatasize())
            let result := delegatecall(gas(), _impl, ptr, calldatasize(), 0, 0)
            let size := returndatasize()
            returndatacopy(ptr, 0, size)
        
            switch result
            case 0 { revert(ptr, size) }
            default { return(ptr, size) }
        }
  }
}
